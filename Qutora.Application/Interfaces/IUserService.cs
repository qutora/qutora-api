using System.Security.Claims;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Authentication;

namespace Qutora.Application.Interfaces;

/// <summary>
/// Service interface for user management
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Lists all users
    /// </summary>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Gets paginated user list
    /// </summary>
    Task<PagedDto<UserDto>> GetPagedUsersAsync(int page = 1, int pageSize = 10, string? searchTerm = null);

    /// <summary>
    /// Gets user information by ID
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Gets user information by email address
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Creates a new user (admin privilege only)
    /// </summary>
    Task<UserDto> CreateUserAsync(CreateUserRequest request);

    /// <summary>
    /// Updates user information
    /// </summary>
    Task<UserDto> UpdateUserAsync(string userId, UserDto user);

    /// <summary>
    /// Changes user password
    /// </summary>
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);

    /// <summary>
    /// Updates user status (active/inactive)
    /// </summary>
    Task<bool> UpdateUserStatusAsync(string userId, bool isActive);

    /// <summary>
    /// Updates user status (active/inactive) with security checks
    /// </summary>
    Task<bool> UpdateUserStatusAsync(string userId, bool isActive, string? currentUserId);

    /// <summary>
    /// Deletes user
    /// </summary>
    Task<bool> DeleteUserAsync(string userId);

    /// <summary>
    /// Deletes user with security checks
    /// </summary>
    Task<bool> DeleteUserAsync(string userId, string? currentUserId);

    /// <summary>
    /// Assigns role to user
    /// </summary>
    Task<bool> AssignRoleToUserAsync(string userId, string roleName);

    /// <summary>
    /// Removes role from user
    /// </summary>
    Task<bool> RemoveRoleFromUserAsync(string userId, string roleName);

    /// <summary>
    /// Gets all roles of the user
    /// </summary>
    Task<IEnumerable<string>> GetUserRolesAsync(string userId);

    /// <summary>
    /// Lists all roles in the system
    /// </summary>
    Task<IEnumerable<string>> GetAllRolesAsync();

    /// <summary>
    /// Lists all roles in the system with ID and Name
    /// </summary>
    Task<IEnumerable<(string Id, string Name)>> GetAllRolesWithIdsAsync();

    /// <summary>
    /// Gets all claims owned by a role
    /// </summary>
    Task<IEnumerable<Claim>> GetRoleClaimsAsync(string roleName);

    /// <summary>
    /// Adds claim to a role
    /// </summary>
    Task<bool> AddClaimToRoleAsync(string roleName, string claimType, string claimValue);

    /// <summary>
    /// Removes claim from a role
    /// </summary>
    Task<bool> RemoveClaimFromRoleAsync(string roleName, string claimType, string claimValue);

    /// <summary>
    /// Returns all permissions in the system in categorized format
    /// </summary>
    Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();

    /// <summary>
    /// Returns permissions owned by a role
    /// </summary>
    Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName);

    /// <summary>
    /// Updates permissions for a role
    /// </summary>
    Task<bool> UpdateRolePermissionsAsync(string roleName, IEnumerable<string> permissions);

    /// <summary>
    /// Creates role
    /// </summary>
    Task<bool> CreateRoleAsync(string roleName, string roleDescription = "");

    /// <summary>
    /// Deletes role
    /// </summary>
    Task<bool> DeleteRoleAsync(string roleName);

    /// <summary>
    /// Gets users in the specified role
    /// </summary>
    Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName);
}