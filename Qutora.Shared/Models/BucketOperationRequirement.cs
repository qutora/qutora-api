using Microsoft.AspNetCore.Authorization;
using Qutora.Shared.Enums;

namespace Qutora.Shared.Models;

/// <summary>
/// Authorization requirement for bucket operations
/// </summary>
public class BucketOperationRequirement(Guid bucketId, PermissionLevel requiredPermission) : IAuthorizationRequirement
{
    /// <summary>
    /// Bucket ID for the operation
    /// </summary>
    public Guid BucketId { get; } = bucketId;

    /// <summary>
    /// Required permission level
    /// </summary>
    public PermissionLevel RequiredPermission { get; } = requiredPermission;
}
