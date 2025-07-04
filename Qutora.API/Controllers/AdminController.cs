using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;
using Qutora.Shared.DTOs;
using Qutora.Shared.DTOs.Authentication;
using Qutora.Shared.DTOs.Common;
using ClaimDto = Qutora.Shared.DTOs.Authentication.ClaimDto;

namespace Qutora.API.Controllers;

/// <summary>
/// Controller for admin operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "UserManagement")]
public class AdminController(IUserService userService, ILogger<AdminController> logger) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<PagedDto<UserDto>>> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null)
    {
        try
        {
            var users = await userService.GetPagedUsersAsync(page, pageSize, searchTerm);
            return Ok(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing users");
            return StatusCode(500, "An error occurred while listing users");
        }
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<UserDto>> GetUserById(string userId)
    {
        try
        {
            var user = await userService.GetUserByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            return Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user information");
            return StatusCode(500, "An error occurred while retrieving user information");
        }
    }

    [HttpPost("users")]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            // Model validation check
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Extra role validation - API level security
            if (request.Roles == null || !request.Roles.Any())
            {
                return BadRequest(MessageResponseDto.ErrorResponse("At least one role must be assigned to the user"));
            }

            // Role existence check
            var availableRoles = await userService.GetAllRolesAsync();
            var invalidRoles = request.Roles.Except(availableRoles).ToList();
            if (invalidRoles.Any())
            {
                return BadRequest(MessageResponseDto.ErrorResponse($"Invalid roles: {string.Join(", ", invalidRoles)}"));
            }

            var user = await userService.CreateUserAsync(request);
            return CreatedAtAction(nameof(GetUserById), new { userId = user.Id }, user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating user");
            return StatusCode(500, "An error occurred while creating user");
        }
    }

    [HttpPut("users/{userId}")]
    public async Task<ActionResult<UserDto>> UpdateUser(string userId, [FromBody] UserDto user)
    {
        try
        {
            if (userId != user.Id) return BadRequest("User IDs do not match");

            var updatedUser = await userService.UpdateUserAsync(userId, user);
            return Ok(updatedUser);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user");
            return StatusCode(500, "An error occurred while updating user");
        }
    }

    [HttpPatch("users/{userId}/status")]
    public async Task<ActionResult<MessageResponseDto>> UpdateUserStatus(string userId, [FromBody] bool isActive)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            var result = await userService.UpdateUserStatusAsync(userId, isActive, currentUserId);
            if (!result) return NotFound(MessageResponseDto.ErrorResponse("User not found"));

            var statusText = isActive ? "active" : "inactive";
            return Ok(MessageResponseDto.SuccessResponse($"User status successfully updated to {statusText}"));
        }
        catch (ApplicationException ex)
        {
            return BadRequest(MessageResponseDto.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user status");
            return StatusCode(500, "An error occurred while updating user status");
        }
    }

    [HttpDelete("users/{userId}")]
    public async Task<ActionResult<MessageResponseDto>> DeleteUser(string userId)
    {
        try
        {
            var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            
            var result = await userService.DeleteUserAsync(userId, currentUserId);
            if (!result) return NotFound(MessageResponseDto.ErrorResponse("User not found"));

            return Ok(MessageResponseDto.SuccessResponse("User successfully deleted"));
        }
        catch (ApplicationException ex)
        {
            return BadRequest(MessageResponseDto.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user");
            return StatusCode(500, "An error occurred while deleting user");
        }
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAllRoles()
    {
        try
        {
            var rolesWithIds = await userService.GetAllRolesWithIdsAsync();
            var roleDtos = rolesWithIds.Select(role => new RoleDto
            {
                Id = role.Id,      // Actual role ID
                Name = role.Name   // Role name
            }).ToList();

            return Ok(roleDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing roles");
            return StatusCode(500, "An error occurred while listing roles");
        }
    }

    [HttpPost("users/{userId}/roles")]
    public async Task<ActionResult<MessageResponseDto>> AssignRoleToUser(string userId, [FromBody] string roleName)
    {
        try
        {
            var result = await userService.AssignRoleToUserAsync(userId, roleName);
            if (!result) return BadRequest(MessageResponseDto.ErrorResponse("Role assignment failed"));

            return Ok(MessageResponseDto.SuccessResponse($"Role '{roleName}' successfully assigned to user"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error assigning role");
            return StatusCode(500, "An error occurred while assigning role");
        }
    }

    [HttpDelete("users/{userId}/roles/{roleName}")]
    public async Task<ActionResult<MessageResponseDto>> RemoveRoleFromUser(string userId, string roleName)
    {
        try
        {
            var result = await userService.RemoveRoleFromUserAsync(userId, roleName);
            if (!result) return BadRequest(MessageResponseDto.ErrorResponse("Role removal failed"));

            return Ok(MessageResponseDto.SuccessResponse($"Role '{roleName}' successfully removed from user"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing role");
            return StatusCode(500, "An error occurred while removing role");
        }
    }

    [HttpGet("users/{userId}/roles")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetUserRoles(string userId)
    {
        try
        {
            var roleNames = await userService.GetUserRolesAsync(userId);
            var roleDtos = roleNames.Select(name => new RoleDto
            {
                Name = name,
                Id = name
            }).ToList();

            return Ok(roleDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing user roles");
            return StatusCode(500, "An error occurred while listing user roles");
        }
    }

    [HttpGet("roles/{roleName}/claims")]
    public async Task<ActionResult<IEnumerable<ClaimDto>>> GetRoleClaims(string roleName)
    {
        try
        {
            var claims = await userService.GetRoleClaimsAsync(roleName);
            return Ok(claims);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing role permissions");
            return StatusCode(500, "An error occurred while listing role permissions");
        }
    }

    [HttpPost("roles/{roleName}/claims")]
    public async Task<ActionResult<MessageResponseDto>> AddClaimToRole(string roleName, [FromBody] ClaimDto claim)
    {
        try
        {
            var result = await userService.AddClaimToRoleAsync(roleName, claim.Type, claim.Value);
            if (!result) return BadRequest(MessageResponseDto.ErrorResponse("Permission addition failed"));

            return Ok(MessageResponseDto.SuccessResponse(
                $"Permission '{claim.Type}:{claim.Value}' successfully added to role"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error adding role permission");
            return StatusCode(500, "An error occurred while adding role permission");
        }
    }

    [HttpDelete("roles/{roleName}/claims")]
    public async Task<ActionResult<MessageResponseDto>> RemoveClaimFromRole(string roleName, [FromQuery] string claimType, [FromQuery] string claimValue)
    {
        try
        {
            if (string.IsNullOrEmpty(claimType) || string.IsNullOrEmpty(claimValue))
            {
                return BadRequest(MessageResponseDto.ErrorResponse("ClaimType and ClaimValue are required"));
            }

            var result = await userService.RemoveClaimFromRoleAsync(roleName, claimType, claimValue);
            if (!result) return BadRequest(MessageResponseDto.ErrorResponse("Permission removal failed"));

            return Ok(MessageResponseDto.SuccessResponse(
                $"Permission '{claimType}:{claimValue}' successfully removed from role"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing role permission");
            return StatusCode(500, "An error occurred while removing role permission");
        }
    }

    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await userService.GetAllPermissionsAsync();
        return Ok(permissions);
    }

    [HttpGet("roles/{roleName}/permissions")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRolePermissions(string roleName)
    {
        var permissions = await userService.GetRolePermissionsAsync(roleName);
        if (permissions == null) return NotFound($"Role not found: {roleName}");

        return Ok(permissions);
    }

    [HttpPut("roles/{roleName}/permissions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRolePermissions(string roleName, [FromBody] List<string> permissions)
    {
        if (string.IsNullOrEmpty(roleName)) return BadRequest("Role name cannot be empty.");

        var result = await userService.UpdateRolePermissionsAsync(roleName, permissions);
        if (!result) return NotFound($"Role not found or permissions not updated: {roleName}");

        return Ok($"Permissions successfully updated for role {roleName}.");
    }

    [HttpPost("roles")]
    public async Task<ActionResult<MessageResponseDto>> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            var result = await userService.CreateRoleAsync(request.Name, request.Description ?? "");
            if (!result) return BadRequest(MessageResponseDto.ErrorResponse("Role creation failed or role already exists"));

            return Ok(MessageResponseDto.SuccessResponse($"Role '{request.Name}' successfully created"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating role");
            return StatusCode(500, "An error occurred while creating role");
        }
    }

    [HttpDelete("roles/{roleName}")]
    public async Task<ActionResult<MessageResponseDto>> DeleteRole(string roleName)
    {
        try
        {
            var result = await userService.DeleteRoleAsync(roleName);
            if (!result) return NotFound(MessageResponseDto.ErrorResponse("Role not found"));

            return Ok(MessageResponseDto.SuccessResponse($"Role '{roleName}' successfully deleted"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(MessageResponseDto.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting role");
            return StatusCode(500, "An error occurred while deleting role");
        }
    }

    [HttpGet("roles/{roleName}/users")]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersInRole(string roleName)
    {
        try
        {
            var users = await userService.GetUsersInRoleAsync(roleName);
            return Ok(users);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting users in role");
            return StatusCode(500, "An error occurred while getting users in role");
        }
    }
}
