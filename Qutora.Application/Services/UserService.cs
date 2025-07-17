using System.Security.Claims;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qutora.Application.Interfaces;
using Qutora.Domain.Entities.Identity;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Authentication;
using Qutora.Shared.Models;

namespace Qutora.Application.Services;

/// <summary>
/// User management service
/// </summary>
public class UserService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IMapper mapper,
    IServiceProvider serviceProvider)
    : IUserService
{
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await userManager.Users.ToListAsync();
        var userDtos = new List<UserDto>();

        foreach (var user in users)
        {
            var userDto = mapper.Map<UserDto>(user);
            var roles = await userManager.GetRolesAsync(user);
            userDto.Roles = roles.ToList();
            userDtos.Add(userDto);
        }

        return userDtos;
    }

    public async Task<PagedDto<UserDto>> GetPagedUsersAsync(int page = 1, int pageSize = 10, string? searchTerm = null)
    {
        var query = userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchLower = searchTerm.ToLower();
            query = query.Where(u => u.FirstName!.ToLower().Contains(searchLower) ||
                                   u.LastName!.ToLower().Contains(searchLower) ||
                                   u.Email!.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userDtos = new List<UserDto>();
        foreach (var user in users)
        {
            var userDto = mapper.Map<UserDto>(user);
            var roles = await userManager.GetRolesAsync(user);
            userDto.Roles = roles.ToList();
            userDtos.Add(userDto);
        }

        return new PagedDto<UserDto>
        {
            Items = userDtos,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var userDto = mapper.Map<UserDto>(user);
        var roles = await userManager.GetRolesAsync(user);
        userDto.Roles = roles.ToList();

        return userDto;
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null) return null;

        var userDto = mapper.Map<UserDto>(user);
        var roles = await userManager.GetRolesAsync(user);
        userDto.Roles = roles.ToList();

        return userDto;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        // First check email existence
        var existingUser = await userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new ApplicationException($"This email address is already in use: {request.Email}");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            // Specific error handling for common cases
            var duplicateUserError = result.Errors.FirstOrDefault(e => e.Code == "DuplicateUserName" || e.Code == "DuplicateEmail");
            if (duplicateUserError != null)
            {
                throw new ApplicationException($"This email address is already in use: {request.Email}");
            }

            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new ApplicationException($"An error occurred while creating user: {string.Join(", ", errors)}");
        }

        if (request.Roles.Count != 0)
            foreach (var role in request.Roles)
                await userManager.AddToRoleAsync(user, role);

        var userDto = mapper.Map<UserDto>(user);
        var roles = await userManager.GetRolesAsync(user);
        userDto.Roles = roles.ToList();

        return userDto;
    }

    public async Task<UserDto> UpdateUserAsync(string userId, UserDto user)
    {
        var existingUser = await userManager.FindByIdAsync(userId);
        if (existingUser == null) throw new ApplicationException("User not found");

        existingUser.Email = user.Email;
        existingUser.UserName = user.Email;
        existingUser.FirstName = user.FirstName;
        existingUser.LastName = user.LastName;
        existingUser.IsActive = user.IsActive;

        var result = await userManager.UpdateAsync(existingUser);

        if (!result.Succeeded)
            throw new ApplicationException(
                $"An error occurred while updating user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        var updatedUser = await userManager.FindByIdAsync(userId);
        if (updatedUser == null) throw new ApplicationException("User not found after update");

        var userDto = mapper.Map<UserDto>(updatedUser);
        var roles = await userManager.GetRolesAsync(updatedUser);
        userDto.Roles = roles.ToList();

        return userDto;
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded;
    }

    public async Task<bool> UpdateUserStatusAsync(string userId, bool isActive)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return false;

        user.IsActive = isActive;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> UpdateUserStatusAsync(string userId, bool isActive, string? currentUserId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return false;

        // Cannot deactivate own account
        if (userId == currentUserId && !isActive)
        {
            throw new ApplicationException("You cannot deactivate your own account!");
        }

        // If user is to be deactivated and has Admin role
        if (!isActive)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            if (userRoles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
            {
                // Check total active admin count in the system
                var allAdmins = await userManager.GetUsersInRoleAsync("Admin");
                var activeAdminCount = allAdmins.Count(u => u.IsActive);

                // If this is the last active admin, cannot be deactivated
                if (activeAdminCount <= 1)
                {
                    throw new ApplicationException("You cannot deactivate the last active admin account! The system would be left without administration.");
                }
            }
        }

        user.IsActive = isActive;
        var result = await userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> DeleteUserAsync(string userId, string? currentUserId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return false;

        // Cannot delete own account
        if (userId == currentUserId)
        {
            throw new ApplicationException("You cannot delete your own account!");
        }

        // If user has Admin role
        var userRoles = await userManager.GetRolesAsync(user);
        if (userRoles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
        {
            // Check total admin count in the system
            var allAdmins = await userManager.GetUsersInRoleAsync("Admin");
            
            // If this is the last admin, cannot be deleted
            if (allAdmins.Count <= 1)
            {
                throw new ApplicationException("You cannot delete the last admin account! The system would be left without administration.");
            }
        }

        var result = await userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> AssignRoleToUserAsync(string userId, string roleName)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return false;

        if (!await roleManager.RoleExistsAsync(roleName)) return false;

        var result = await userManager.AddToRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<bool> RemoveRoleFromUserAsync(string userId, string roleName)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return false;

        var result = await userManager.RemoveFromRoleAsync(user, roleName);
        return result.Succeeded;
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return [];

        return await userManager.GetRolesAsync(user);
    }

    public async Task<IEnumerable<string>> GetAllRolesAsync()
    {
        var roles = await roleManager.Roles.ToListAsync();
        return roles.Select(r => r.Name!).Where(n => !string.IsNullOrEmpty(n));
    }

    public async Task<IEnumerable<(string Id, string Name)>> GetAllRolesWithIdsAsync()
    {
        var roles = await roleManager.Roles.ToListAsync();
        return roles.Where(r => !string.IsNullOrEmpty(r.Name)).Select(r => (r.Id, r.Name!));
    }

    public async Task<IEnumerable<Claim>> GetRoleClaimsAsync(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return [];

        return await roleManager.GetClaimsAsync(role);
    }

    public async Task<bool> AddClaimToRoleAsync(string roleName, string claimType, string claimValue)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return false;

        var result = await roleManager.AddClaimAsync(role, new Claim(claimType, claimValue));
        return result.Succeeded;
    }

    public async Task<bool> RemoveClaimFromRoleAsync(string roleName, string claimType, string claimValue)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return false;

        var result = await roleManager.RemoveClaimAsync(role, new Claim(claimType, claimValue));
        return result.Succeeded;
    }

    public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
    {
        var permissionList = new List<PermissionDto>();
        Dictionary<string, string> sections;
        Dictionary<string, string> actionDescriptions;

        using (var scope = serviceProvider.CreateScope())
        {
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            sections = configuration.GetSection("Authorization:PermissionDefinitions:Sections")
                           .Get<Dictionary<string, string>>()
                       ?? new Dictionary<string, string>();

            actionDescriptions = configuration.GetSection("Authorization:PermissionDefinitions:Actions")
                                     .Get<Dictionary<string, string>>()
                                 ?? new Dictionary<string, string>();

            var policies = configuration.GetSection("Authorization:Policies").Get<List<AuthorizationPolicy>>();

            if (policies != null)
                foreach (var policy in policies)
                    if (policy.RequiredPermissions != null && policy.RequiredPermissions.Any())
                        foreach (var permission in policy.RequiredPermissions)
                        {
                            var parts = permission.Split('.');
                            if (parts.Length == 2)
                            {
                                var area = parts[0];
                                var action = parts[1];

                                var category = sections.TryGetValue(area, out var sectionName) ? sectionName : "Other";
                                var actionDesc = actionDescriptions.TryGetValue(action, out var actionName)
                                    ? actionName
                                    : action;

                                var permissionDto = new PermissionDto
                                {
                                    Name = permission,
                                    DisplayName = $"{area} {actionDesc}",
                                    Description = $"{area} {actionDesc.ToLower()} permission",
                                    Category = category
                                };

                                if (!permissionList.Any(p => p.Name == permission)) permissionList.Add(permissionDto);
                            }
                        }
        }

        return permissionList.OrderBy(p => p.Category).ThenBy(p => p.DisplayName);
    }

    public async Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return [];

        var claims = await roleManager.GetClaimsAsync(role);
        return claims.Where(c => c.Type == "permissions").Select(c => c.Value).ToList();
    }

    public async Task<bool> UpdateRolePermissionsAsync(string roleName, IEnumerable<string> permissions)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return false;

        // Permission validation
        var validPermissions = await GetValidPermissionsAsync();
        var validPermissionNames = validPermissions.Select(p => p.Name).ToHashSet();
        
        // Filter only valid permissions
        var validInputPermissions = permissions.Where(p => validPermissionNames.Contains(p)).ToList();
        
        // If no valid permissions exist and permissions were provided, return false
        if (!validInputPermissions.Any() && permissions.Any())
        {
            return false; // Invalid permissions provided
        }

        var currentClaims = await roleManager.GetClaimsAsync(role);
        var permissionClaims = currentClaims.Where(c => c.Type == "permissions").ToList();

        foreach (var claim in permissionClaims)
        {
            var result = await roleManager.RemoveClaimAsync(role, claim);
            if (!result.Succeeded) return false;
        }

        foreach (var permission in validInputPermissions)
        {
            var claim = new Claim("permissions", permission);
            var result = await roleManager.AddClaimAsync(role, claim);
            if (!result.Succeeded) return false;
        }

        return true;
    }

    private async Task<IEnumerable<PermissionDto>> GetValidPermissionsAsync()
    {
        // Use existing GetAllPermissionsAsync method
        return await GetAllPermissionsAsync();
    }

    public async Task<bool> CreateRoleAsync(string roleName, string roleDescription = "")
    {
        if (await roleManager.RoleExistsAsync(roleName))
            return false;

        var role = new ApplicationRole
        {
            Name = roleName,
            Description = roleDescription
        };

        var result = await roleManager.CreateAsync(role);
        return result.Succeeded;
    }

    public async Task<bool> DeleteRoleAsync(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role == null) return false;

        var usersInRole = await userManager.GetUsersInRoleAsync(roleName);
        if (usersInRole.Any())
            throw new InvalidOperationException("Cannot delete role that has users assigned to it.");

        var result = await roleManager.DeleteAsync(role);
        return result.Succeeded;
    }

    public async Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName)
    {
        var users = await userManager.GetUsersInRoleAsync(roleName);
        return users.Select(u => new UserDto
        {
            Id = u.Id,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            FirstName = u.FirstName,
            LastName = u.LastName,
            IsActive = u.IsActive
        }).ToList();
    }
}
