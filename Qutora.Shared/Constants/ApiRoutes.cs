namespace Qutora.Shared.Constants;

/// <summary>
/// Centralized API route constants for consistent URL management
/// </summary>
public static class ApiRoutes
{
    /// <summary>
    /// Document management endpoints
    /// </summary>
    public static class Documents
    {
        public const string Base = "api/documents";
        public const string GetById = "api/documents/{0}";
        public const string GetByCategory = "api/documents/category/{0}";
        public const string Download = "api/documents/{0}/download";
        public const string Metadata = "api/documents/{0}/metadata";
        public const string StorageProviders = "api/documents/storage/providers";
        
        // Document versions as sub-resource
        public const string Versions = "api/documents/{0}/versions";
        public const string VersionDownload = "api/documents/{0}/versions/{1}/download";
        public const string VersionRollback = "api/documents/{0}/versions/{1}/rollback";
        public const string VersionDetails = "api/documents/{0}/versions/{1}/details";
        
        // Document shares as sub-resource
        public const string Shares = "api/documents/{0}/shares";
        public const string ShareById = "api/documents/{0}/shares/{1}";
        public const string ShareToggleStatus = "api/documents/{0}/shares/{1}/toggle-status";
    }

    /// <summary>
    /// Metadata management endpoints
    /// </summary>
    public static class Metadata
    {
        public const string Base = "api/metadata";
        public const string GetByDocument = "api/metadata/document/{0}";
        public const string CreateForDocument = "api/metadata/document/{0}";
        public const string UpdateForDocument = "api/metadata/document/{0}";
        public const string DeleteById = "api/metadata/{0}";
        public const string GetBySchema = "api/metadata/schema/{0}";
        public const string Search = "api/metadata/search";
        public const string GetByTags = "api/metadata/tags";
        public const string Validate = "api/metadata/validate";
        
        // Metadata schemas as sub-resource
        public static class Schemas
        {
            public const string Base = "api/metadata/schemas";
            public const string GetById = "api/metadata/schemas/{0}";
            public const string GetByName = "api/metadata/schemas/name/{0}";
            public const string GetByCategory = "api/metadata/schemas/category/{0}";
            public const string GetByFileType = "api/metadata/schemas/filetype/{0}";
            public const string GetAll = "api/metadata/schemas/all";
        }
    }

    /// <summary>
    /// Category management endpoints
    /// </summary>
    public static class Categories
    {
        public const string Base = "api/categories";
        public const string GetById = "api/categories/{0}";
        public const string Root = "api/categories/root";
        public const string SubCategories = "api/categories/{0}/subcategories";
    }

    /// <summary>
    /// Storage management endpoints (namespaced)
    /// </summary>
    public static class Storage
    {
        /// <summary>
        /// Storage provider endpoints
        /// </summary>
        public static class Providers
        {
            public const string Base = "api/storage/providers";
            public const string GetById = "api/storage/providers/{0}";
            public const string Active = "api/storage/providers/active";
            public const string TestConnection = "api/storage/providers/test-connection";
            public const string TestConnectionById = "api/storage/providers/test-connection/{0}";
            public const string SetDefault = "api/storage/providers/{0}/default";
            public const string ToggleStatus = "api/storage/providers/{0}/status";
            public const string Available = "api/storage/providers/available";
            public const string ProviderTypes = "api/storage/providers/types";
            public const string ConfigSchema = "api/storage/providers/config-schema/{0}";
            public const string AllConfigSchemas = "api/storage/providers/config-schemas";
            public const string Capabilities = "api/storage/providers/{0}/capabilities";
            public const string UserAccessible = "api/storage/providers/user-accessible";
        }

        /// <summary>
        /// Storage bucket endpoints
        /// </summary>
        public static class Buckets
        {
            public const string Base = "api/storage/buckets";
            public const string Provider = "api/storage/buckets/provider/{0}";
            public const string Exists = "api/storage/buckets/exists";
            public const string GetById = "api/storage/buckets/{0}";
            public const string Delete = "api/storage/buckets/{0}";
            public const string Permissions = "api/storage/buckets/{0}/permissions";
            public const string MyAccessibleBuckets = "api/storage/buckets/my-accessible-buckets";
            public const string DeletePermission = "api/storage/buckets/permissions/{0}";
        }

        /// <summary>
        /// Storage permission endpoints
        /// </summary>
        public static class Permissions
        {
            public const string Base = "api/storage/permissions";
            public const string Check = "api/storage/permissions/check/{0}";
            public const string GetByBucket = "api/storage/permissions/bucket/{0}";
            public const string GetByUser = "api/storage/permissions/user/{0}";
            public const string MyPermissions = "api/storage/permissions/my-permissions";
            public const string Grant = "api/storage/permissions";
            public const string Revoke = "api/storage/permissions/{0}";
            public const string GrantApiKey = "api/storage/permissions/api-key";
            public const string RevokeApiKey = "api/storage/permissions/api-key/{0}";
            public const string GetApiKeyPermissions = "api/storage/permissions/api-key/{0}";
            public const string GetAllUserPermissions = "api/storage/permissions/user";
        }
    }

    /// <summary>
    /// Authentication and authorization endpoints
    /// </summary>
    public static class Auth
    {
        public const string Base = "api/auth";
        public const string Login = "api/auth/login";
        public const string InitialSetup = "api/auth/initial-setup";
        public const string SystemStatus = "api/auth/system-status";
        public const string RefreshToken = "api/auth/refresh-token";
        public const string Logout = "api/auth/logout";
        public const string User = "api/auth/user";
        
        // Profile management
        public const string Profile = "api/auth/profile";
        public const string UpdateProfile = "api/auth/profile";
        public const string ChangePassword = "api/auth/change-password";

        /// <summary>
        /// API Key management endpoints
        /// </summary>
        public static class ApiKeys
        {
            public const string Base = "api/auth/api-keys";
            public const string GetById = "api/auth/api-keys/{0}";
            public const string Create = "api/auth/api-keys";
            public const string Update = "api/auth/api-keys/{0}";
            public const string Delete = "api/auth/api-keys/{0}";
            public const string Deactivate = "api/auth/api-keys/{0}/deactivate";
            public const string Activity = "api/auth/api-keys/{0}/activity";
        }
    }

    /// <summary>
    /// System management endpoints
    /// </summary>
    public static class System
    {
        /// <summary>
        /// Admin endpoints
        /// </summary>
        public static class Admin
        {
            public const string Base = "api/admin";
            public const string GetAllUsers = "api/admin/users";
            public const string GetUserById = "api/admin/users/{0}";
            public const string CreateUser = "api/admin/users";
            public const string UpdateUser = "api/admin/users/{0}";
            public const string UpdateUserStatus = "api/admin/users/{0}/status";
            public const string DeleteUser = "api/admin/users/{0}";
            public const string GetAllRoles = "api/admin/roles";
            public const string AssignRoleToUser = "api/admin/users/{0}/roles";
            public const string RemoveRoleFromUser = "api/admin/users/{0}/roles/{1}";
            public const string GetUserRoles = "api/admin/users/{0}/roles";
            public const string GetRoleClaims = "api/admin/roles/{0}/claims";
            public const string AddClaimToRole = "api/admin/roles/{0}/claims";
            public const string RemoveClaimFromRole = "api/admin/roles/{0}/claims";
            public const string GetAllPermissions = "api/admin/permissions";
            public const string GetRolePermissions = "api/admin/roles/{0}/permissions";
            public const string UpdateRolePermissions = "api/admin/roles/{0}/permissions";
            public const string CreateRole = "api/admin/roles";
            public const string DeleteRole = "api/admin/roles/{0}";
            public const string GetUsersInRole = "api/admin/roles/{0}/users";
        }

        /// <summary>
        /// Email system endpoints
        /// </summary>
        public static class Email
        {
            public const string Base = "api/system/email";
        }

        /// <summary>
        /// Dashboard endpoints
        /// </summary>
        public static class Dashboard
        {
            public const string Base = "api/dashboard";
            public const string Stats = "api/dashboard/stats";
            public const string DocumentStats = "api/dashboard/document-stats";
            public const string UserDocumentStats = "api/dashboard/user-document-stats";
            public const string UserStats = "api/dashboard/user-stats";
            public const string ApprovalStats = "api/dashboard/approval-stats";
            public const string ApiKeyStats = "api/dashboard/api-key-stats";
            public const string UserApiKeyStats = "api/dashboard/user-api-key-stats";
            public const string SharingStats = "api/dashboard/sharing-stats";
            public const string UserSharingStats = "api/dashboard/user-sharing-stats";
            public const string RecentDocuments = "api/dashboard/recent-documents";
            public const string RecentActivities = "api/dashboard/recent-activities";
        }
    }

    /// <summary>
    /// Approval system endpoints
    /// </summary>
    public static class Approval
    {
        public const string Base = "api/approval";
        public const string Requests = "api/approval/requests";
        public const string RequestById = "api/approval/requests/{0}";
        public const string ProcessRequest = "api/approval/requests/{0}/process";
        public const string RequestHistory = "api/approval/requests/{0}/history";
        public const string PendingRequests = "api/approval/requests/pending";
        public const string MyRequests = "api/approval/requests/my-requests";
        public const string Policies = "api/approval/policies";
        public const string PolicyById = "api/approval/policies/{0}";
        public const string TogglePolicyStatus = "api/approval/policies/{0}/toggle";
        public const string PolicyTest = "api/approval/policies/{0}/test";
        public const string Settings = "api/approval/settings";
        public const string Statistics = "api/approval/dashboard/statistics";
        public const string Dashboard = "api/approval/dashboard";
        public const string EnableGlobalApproval = "api/approval/settings/enable-global";
        public const string DisableGlobalApproval = "api/approval/settings/disable-global";
    }

    /// <summary>
    /// Document sharing endpoints
    /// </summary>
    public static class DocumentShares
    {
        public const string Base = "api/document-shares";
        public const string GetAll = "api/document-shares/my-shares";
        public const string GetById = "api/document-shares/{0}";
        public const string Update = "api/document-shares/{0}";
        public const string Delete = "api/document-shares/{0}";
        public const string ToggleStatus = "api/document-shares/{0}/toggle-status";
        public const string MyShares = "api/document-shares/my-shares";
        public const string ViewTrend = "api/document-shares/view-trend";
    }

    /// <summary>
    /// Special endpoints (keep as-is for compatibility)
    /// </summary>
    public static class Special
    {
        public const string SharedDocuments = "api/shared-documents";
        public const string DirectAccess = "api/direct-access";
    }
} 