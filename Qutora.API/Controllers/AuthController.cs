using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Interfaces;
using Qutora.Shared.DTOs.Authentication;
using Qutora.Shared.DTOs.Common;

namespace Qutora.API.Controllers;

/// <summary>
/// Controller for authentication operations
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await authService.LoginAsync(request);
            if (!response.Success) return Unauthorized(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during login");
            return StatusCode(500, "An error occurred during login");
        }
    }

    [HttpPost("initial-setup")]
    [AllowAnonymous]
    public async Task<ActionResult<MessageResponseDto>> InitialSetup([FromBody] InitialSetupRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var isInitialized = await authService.IsSystemInitializedAsync();
            if (isInitialized)
                return BadRequest(
                    MessageResponseDto.ErrorResponse(
                        "System is already initialized. This endpoint is no longer available."));

            var setupResult = await authService.InitialSetupAsync(request);
            if (!setupResult.Success) return BadRequest(MessageResponseDto.ErrorResponse(setupResult.Message));

            return Ok(MessageResponseDto.SuccessResponse("System setup completed successfully. Please log in."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during initial setup");
            return StatusCode(500, "An error occurred during system setup");
        }
    }

    [HttpGet("system-status")]
    [AllowAnonymous]
    public async Task<ActionResult<SystemStatusDto>> GetSystemStatus()
    {
        try
        {
            var isInitialized = await authService.IsSystemInitializedAsync();

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            var statusDto = new SystemStatusDto
            {
                IsInitialized = isInitialized,
                Version = version,
                Timestamp = DateTime.UtcNow
            };

            return Ok(statusDto);
        }
        catch (Exception ex)
        {
            
              logger.LogError(ex, "Error checking system initialization status");
            return StatusCode(500, "An error occurred while checking system status");
        }
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var response = await authService.RefreshTokenAsync(request);
            if (!response.Success) return Unauthorized(response);
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during token refresh");
            return StatusCode(500, "An error occurred during token refresh");
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<MessageResponseDto>> Logout()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return BadRequest(MessageResponseDto.ErrorResponse("User ID not found"));

            var result = await authService.LogoutAsync(userId);
            if (!result) return BadRequest(MessageResponseDto.ErrorResponse("Logout failed"));

            return Ok(MessageResponseDto.SuccessResponse("Successfully logged out"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during logout");
            return StatusCode(500, "An error occurred during logout");
        }
    }

    [HttpGet("user")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID not found");

            var user = await authService.GetUserByIdAsync(userId);
            if (user == null) return NotFound("User not found");

            return Ok(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user information");
            return StatusCode(500, "An error occurred while retrieving user information");
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID not found");

            var profile = await authService.GetUserProfileAsync(userId);
            if (profile == null) return NotFound("Profile not found");

            return Ok(profile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, "An error occurred while retrieving user profile");
        }
    }

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<MessageResponseDto>> UpdateProfile([FromBody] UpdateUserProfileDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID not found");

            var result = await authService.UpdateUserProfileAsync(userId, request);
            if (!result.Success) return BadRequest(MessageResponseDto.ErrorResponse(result.Message));

            return Ok(MessageResponseDto.SuccessResponse("Profile updated successfully"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, "An error occurred while updating profile");
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<MessageResponseDto>> ChangePassword([FromBody] ChangePasswordDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return BadRequest("User ID not found");

            var result = await authService.ChangePasswordAsync(userId, request);
            if (!result.Success) return BadRequest(MessageResponseDto.ErrorResponse(result.Message));

            return Ok(MessageResponseDto.SuccessResponse("Password changed successfully"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing password");
            return StatusCode(500, "An error occurred while changing password");
        }
    }
}
