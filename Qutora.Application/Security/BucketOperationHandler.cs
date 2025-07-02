using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Qutora.Application.Interfaces;
using Qutora.Infrastructure.Security.Authorization;

namespace Qutora.Application.Security;

/// <summary>
/// Bucket operations authorization handler
/// </summary>
public class BucketOperationHandler(IServiceScopeFactory serviceScopeFactory)
    : AuthorizationHandler<BucketOperationRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BucketOperationRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId)) return;

        var hasAdminAccess = context.User.HasClaim("permissions", "Admin.Access");
        var hasBucketManage = context.User.HasClaim("permissions", "Bucket.Manage");
        
        if (hasAdminAccess || hasBucketManage)
        {
            context.Succeed(requirement);
            return;
        }

        using (var scope = serviceScopeFactory.CreateScope())
        {
            var permissionManager = scope.ServiceProvider.GetRequiredService<IBucketPermissionManager>();

            var permissionCheck = await permissionManager.CheckUserBucketOperationPermissionAsync(
                userId, requirement.BucketId, requirement.RequiredPermission);

            if (permissionCheck.IsAllowed) context.Succeed(requirement);
        }
    }
}
