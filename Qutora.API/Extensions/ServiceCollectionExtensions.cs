using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Qutora.API.Authentication;
using Qutora.Application.Mappings;
using Qutora.Database.Abstractions;
using Qutora.Database.MySQL;
using Qutora.Database.PostgreSQL;
using Qutora.Database.SqlServer;
using Qutora.Domain.Entities.Identity;
using Qutora.Infrastructure.Persistence.Repositories;
using Qutora.Infrastructure.Persistence.UnitOfWork;
using Qutora.Infrastructure.Persistence.Transactions;
using Qutora.Infrastructure.Services;
using Qutora.Infrastructure.Storage.Models;
using Qutora.Infrastructure.Storage.Providers;
using MapsterMapper;
using Qutora.Application.Extensions;
using Qutora.Application.Identity;
using Qutora.Application.Interfaces;
using Qutora.Application.Interfaces.Repositories;
using Qutora.Application.Interfaces.Storage;
using Qutora.Application.Interfaces.UnitOfWork;
using Qutora.Application.Security;
using Qutora.Application.Services;
using Qutora.Application.Startup;
using Qutora.Infrastructure.Extensions;
using Qutora.Infrastructure.Storage;
using Qutora.Infrastructure.Persistence;
using AuthorizationPolicy = Qutora.Shared.Models.AuthorizationPolicy;

namespace Qutora.API.Extensions;

/// <summary>
/// Extension methods for service registration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers database services
    /// </summary>
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
    {
        var registry = new DbProviderRegistry();
        services.AddSingleton<IDbProviderRegistry>(registry);

        var providerName = configuration["Database:Provider"] ?? "SqlServer";

        if (providerName.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            registry.RegisterProvider(new PostgreSqlProvider());
        else if (providerName.Equals("MySQL", StringComparison.OrdinalIgnoreCase))
            registry.RegisterProvider(new MySqlProvider());
        else
            registry.RegisterProvider(new SqlServerProvider());

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                   throw new InvalidOperationException("Connection string is not configured.");

            var dbProvider = registry.GetProvider(providerName);

            if (dbProvider == null)
                throw new InvalidOperationException($"Database provider '{providerName}' is not registered.");

            dbProvider.ConfigureDbContext(options, connectionString);
        });

        services.AddScoped<ApplicationDbContextInitializer>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ApplicationDbContextInitializer>>();
            var context = provider.GetRequiredService<ApplicationDbContext>();

            return new ApplicationDbContextInitializer(logger, context, registry, providerName);
        });

        services.AddRepositoryServices(configuration);

        return services;
    }

    /// <summary>
    /// Registers Repository and UnitOfWork services
    /// </summary>
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IDbProvider>(provider =>
        {
            var registry = provider.GetRequiredService<IDbProviderRegistry>();
            var providerName = configuration["Database:Provider"] ?? "SqlServer";
            var dbProvider = registry.GetProvider(providerName);

            if (dbProvider == null)
                throw new InvalidOperationException($"Database provider '{providerName}' not found in registry.");

            return dbProvider;
        });

        services.AddScoped<ApplicationDbContext>();
        services.AddScoped<ITransactionManager, TransactionManager>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDbProviderUnitOfWork, DbProviderUnitOfWork>();
        services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IMetadataRepository, MetadataRepository>();
        services.AddScoped<IMetadataSchemaRepository, MetadataSchemaRepository>();
        services.AddScoped<IDocumentVersionRepository, DocumentVersionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IStorageProviderRepository, StorageProviderRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<IStorageBucketRepository, StorageBucketRepository>();
        services.AddScoped<IBucketPermissionRepository, BucketPermissionRepository>();
        services.AddScoped<IApiKeyBucketPermissionRepository, ApiKeyBucketPermissionRepository>();
        services.AddScoped<IDocumentShareRepository, DocumentShareRepository>();
        services.AddScoped<IDocumentShareViewRepository, DocumentShareViewRepository>();

        services.AddScoped<IApprovalSettingsRepository, ApprovalSettingsRepository>();
        services.AddScoped<IApprovalPolicyRepository, ApprovalPolicyRepository>();
        services.AddScoped<IShareApprovalRequestRepository, ShareApprovalRequestRepository>();
        services.AddScoped<IApprovalDecisionRepository, ApprovalDecisionRepository>();
        services.AddScoped<IApprovalHistoryRepository, ApprovalHistoryRepository>();

        services.AddScoped<IEmailSettingsRepository, EmailSettingsRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();

        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISystemSettingsRepository, SystemSettingsRepository>();

        return services;
    }

    /// <summary>
    /// Registers Identity services
    /// </summary>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;

                options.User.RequireUniqueEmail = true;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return services;
    }

    /// <summary>
    /// Registers Storage services
    /// </summary>
    public static IServiceCollection AddStorageServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.AddSingleton<IStorageCapabilityCache, StorageCapabilityCache>();
        services
            .AddSingleton<IStorageProviderTypeRegistry, Infrastructure.Storage.Registry.StorageProviderTypeRegistry>();
        services.AddHostedService<Infrastructure.Storage.Registry.StorageProviderRegistryInitializer>();

        services.AddSingleton<Infrastructure.Storage.Registry.StorageProviderFactory>();

        // HttpClientFactory for Minio and other HTTP-based providers
        services.AddHttpClient("minio")
            .ConfigureHttpClient(client => { client.Timeout = TimeSpan.FromMinutes(5); })
            .SetHandlerLifetime(TimeSpan.FromMinutes(10));

        services.AddScoped<IStorageProviderRepository, StorageProviderRepository>();
        services.AddSingleton<IStorageManager, StorageManager>();
        services.AddScoped<IStorageProviderService, StorageProviderService>();

        services.AddScoped<IFileStorageService, FileStorageAdapter>();

        services.AddScoped<IAuditService, AuditService>();

        services.AddScoped<IDocumentVersionService, DocumentVersionService>();

        services.AddScoped<IDocumentService, DocumentService>();

        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<IMetadataService, MetadataService>();

        services.AddScoped<IMetadataSchemaService, MetadataSchemaService>();

        services.AddScoped<IStorageBucketService, StorageBucketService>();

        services.AddScoped<IDocumentShareService, DocumentShareService>();

        services.AddScoped<IPasswordHashingService, PasswordHashingService>();

        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IApprovalPolicyService, ApprovalPolicyService>();
        services.AddScoped<IApprovalSettingsService, ApprovalSettingsService>();
        services.AddScoped<IApprovalEmailService, ApprovalEmailService>();
        services.AddScoped<IDocumentShareEmailService, DocumentShareEmailService>();
        
        // Dashboard service
        services.AddScoped<IDashboardService, DashboardService>();
        
        // Event system
        services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
        services.AddHostedService<EmailJobBackgroundService>();

        // Document orchestration services
        services.AddScoped<IDocumentValidationService, DocumentValidationService>();
        services.AddScoped<IDocumentAuthorizationService, DocumentAuthorizationService>();
        services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        services.AddScoped<IDocumentOrchestrationService, DocumentOrchestrationService>();

        services.AddHostedService<ApprovalBackgroundService>();

        services.AddHostedService<SystemInitializationService>();

        // ✨ API Key Caching System
        services.AddApiKeyCaching();
        services.AddApiKeyCacheHealthChecks();

        return services;
    }

    /// <summary>
    /// Registers Security services (Authentication, Authorization, API Key)
    /// </summary>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettingsSection = configuration.GetSection("Jwt");
        services.Configure<JwtSettings>(jwtSettingsSection);

        var jwtSettings = jwtSettingsSection.Get<JwtSettings>();
        if (jwtSettings == null) throw new InvalidOperationException("JWT settings not configured.");

        var key = Encoding.ASCII.GetBytes(jwtSettings.Key);

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = jwtSettings.RequireHttpsMetadata;
                options.SaveToken = jwtSettings.SaveToken;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = jwtSettings.ValidateIssuer,
                    ValidateAudience = jwtSettings.ValidateAudience,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = jwtSettings.ValidateLifetime,
                    ClockSkew = TimeSpan.FromMinutes(jwtSettings.AccessTokenClockSkew)
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", null);

        services.Configure<ApiKeyPermissionSettings>(configuration.GetSection("ApiKeyPermissions"));
        services.AddScoped<IAuthService, AuthService>();

        if (jwtSettings.TokenBlacklistEnabled)
            services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
        else
            services.AddSingleton<ITokenBlacklistService, NoOpTokenBlacklistService>();

        services.AddScoped<IUserService, UserService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IApiKeyService, ApiKeyService>();

        services.AddAuthorization(options =>
        {
            // Load policies from appsettings.json
            var policies = configuration.GetSection("Authorization:Policies").Get<List<AuthorizationPolicy>>();
            if (policies != null)
                foreach (var policy in policies)
                    options.AddPolicy(policy.Name, builder =>
                    {
                        if (policy.RequiresAuthenticatedUser) builder.RequireAuthenticatedUser();

                        if (policy.RequiredPermissions != null && policy.RequiredPermissions.Any())
                            // Check each permission individually as they are stored as role claims
                            foreach (var permission in policy.RequiredPermissions)
                                builder.RequireClaim("permissions", permission);

                        if (policy.AuthenticationSchemes != null && policy.AuthenticationSchemes.Any())
                            builder.AddAuthenticationSchemes(policy.AuthenticationSchemes.ToArray());
                    });
        });

        services.AddScoped<IBucketPermissionManager, BucketPermissionManager>();
        services.AddSingleton<IAuthorizationHandler, BucketOperationHandler>();

        return services;
    }

    /// <summary>
    /// Registers API services
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Handle circular references with ReferenceHandler.Preserve
                options.JsonSerializerOptions.ReferenceHandler =
                    System.Text.Json.Serialization.ReferenceHandler.Preserve;
            });

        services.AddEndpointsApiExplorer();

        var config = MappingConfig.RegisterMappings();
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();


        return services;
    }
}
