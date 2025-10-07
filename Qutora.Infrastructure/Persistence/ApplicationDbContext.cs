using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Qutora.Domain.Base;
using Qutora.Domain.Entities;
using Qutora.Domain.Entities.Identity;

namespace Qutora.Infrastructure.Persistence;

/// <summary>
/// Database context class for ASP.NET Identity
/// </summary>
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<SystemSettings> SystemSettings { get; set; } = null!;
    public DbSet<StorageProvider> StorageProviders { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<Metadata> Metadata { get; set; } = null!;
    public DbSet<MetadataSchema> MetadataSchemas { get; set; } = null!;
    public DbSet<DocumentVersion> DocumentVersions { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<StorageBucket> StorageBuckets { get; set; } = null!;
    public DbSet<BucketPermission> BucketPermissions { get; set; } = null!;
    public DbSet<ApiKeyBucketPermission> ApiKeyBucketPermissions { get; set; } = null!;
    public DbSet<DocumentShare> DocumentShares { get; set; } = null!;
    public DbSet<DocumentShareView> DocumentShareViews { get; set; } = null!;

    public DbSet<ApprovalSettings> ApprovalSettings { get; set; } = null!;
    public DbSet<ApprovalPolicy> ApprovalPolicies { get; set; } = null!;
    public DbSet<ShareApprovalRequest> ShareApprovalRequests { get; set; } = null!;
    public DbSet<ApprovalDecision> ApprovalDecisions { get; set; } = null!;
    public DbSet<ApprovalHistory> ApprovalHistories { get; set; } = null!;
    public DbSet<EmailSettings> EmailSettings { get; set; } = null!;
    public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Detect database provider
        var isPostgreSql = Database.ProviderName?.Contains("Npgsql") ?? false;
        
        // Configure BaseEntity audit fields for all entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // CreatedBy field configuration
                var createdByProperty = entityType.FindProperty(nameof(BaseEntity.CreatedBy));
                if (createdByProperty != null)
                {
                    createdByProperty.SetMaxLength(450); // Match AspNetUsers.Id length
                }

                // UpdatedBy field configuration  
                var updatedByProperty = entityType.FindProperty(nameof(BaseEntity.UpdatedBy));
                if (updatedByProperty != null)
                {
                    updatedByProperty.SetMaxLength(450); // Match AspNetUsers.Id length
                }

                // PostgreSQL-specific: Configure RowVersion 
                // PostgreSQL doesn't auto-generate bytea values like SQL Server's rowversion
                // We'll use a trigger or let EF handle it with default empty array
                if (isPostgreSql)
                {
                    var rowVersionProperty = entityType.FindProperty(nameof(BaseEntity.RowVersion));
                    if (rowVersionProperty != null)
                    {
                        // Make it optional for PostgreSQL
                        rowVersionProperty.IsNullable = true;
                        // Set a default value
                        rowVersionProperty.SetDefaultValue(new byte[] { 0, 0, 0, 0 });
                    }
                }

                // Configure navigation properties
                modelBuilder.Entity(entityType.ClrType)
                    .HasOne(typeof(ApplicationUser), nameof(BaseEntity.CreatedByUser))
                    .WithMany()
                    .HasForeignKey(nameof(BaseEntity.CreatedBy))
                    .OnDelete(DeleteBehavior.Restrict);

                modelBuilder.Entity(entityType.ClrType)
                    .HasOne(typeof(ApplicationUser), nameof(BaseEntity.UpdatedByUser))
                    .WithMany()
                    .HasForeignKey(nameof(BaseEntity.UpdatedBy))
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }

        var stringArrayComparer = new ValueComparer<string[]>(
            (c1, c2) => c1!.SequenceEqual(c2!),
            c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c.ToArray());

        var guidCollectionComparer = new ValueComparer<ICollection<Guid>>(
            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
            c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
            c => c != null ? new List<Guid>(c) : new List<Guid>()
        );

        modelBuilder.Entity<SystemSettings>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<SystemSettings>()
            .HasData(new SystemSettings
            {
                Id = 1,
                IsInitialized = false,
                Version = "1.0.0",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<StorageProvider>()
            .HasKey(p => p.Id);

        // Storage providers will be created during initial setup, not as seed data

        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Category)
            .WithMany(c => c.Documents)
            .HasForeignKey(d => d.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Document CreatedBy navigation will be overridden to use specific relationship
        modelBuilder.Entity<Document>()
            .HasOne(d => d.CreatedByUser)
            .WithMany(u => u.CreatedDocuments)
            .HasForeignKey(d => d.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.StorageProvider)
            .WithMany()
            .HasForeignKey(d => d.StorageProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasMany(d => d.Versions)
            .WithOne(v => v.Document)
            .HasForeignKey(v => v.DocumentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.CurrentVersion)
            .WithMany()
            .HasForeignKey(d => d.CurrentVersionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Metadata>()
            .HasOne(m => m.Document)
            .WithOne(d => d.Metadata)
            .HasForeignKey<Metadata>(m => m.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<MetadataSchema>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<ApiKey>()
            .HasKey(k => k.Id);

        modelBuilder.Entity<ApiKey>()
            .HasIndex(k => k.Key)
            .IsUnique();

        // Performance indexes for ApiKey
        modelBuilder.Entity<ApiKey>()
            .HasIndex(k => new { k.IsActive, k.IsDeleted })
            .HasDatabaseName("IX_ApiKeys_IsActive_IsDeleted");

        modelBuilder.Entity<ApiKey>()
            .HasIndex(k => new { k.LastUsedAt, k.IsActive })
            .HasFilter(Database.IsSqlServer() ? "[IsDeleted] = 0" : 
                      Database.IsMySql() ? "`IsDeleted` = 0" : 
                      "\"IsDeleted\" = false")
            .HasDatabaseName("IX_ApiKeys_LastUsedAt_IsActive");

        modelBuilder.Entity<ApiKey>()
            .Property(k => k.AllowedProviderIds)
            .HasConversion(
                v => string.Join(',', v.Select(id => id.ToString())),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => Guid.Parse(id))
                    .ToList())
            .Metadata.SetValueComparer(guidCollectionComparer);

        modelBuilder.Entity<StorageBucket>()
            .HasKey(b => b.Id);

        modelBuilder.Entity<StorageBucket>()
            .HasOne(b => b.Provider)
            .WithMany()
            .HasForeignKey(b => b.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Storage buckets will be created during initial setup, not as seed data

        modelBuilder.Entity<BucketPermission>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<BucketPermission>()
            .HasOne(p => p.Bucket)
            .WithMany(b => b.Permissions)
            .HasForeignKey(p => p.BucketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApiKeyBucketPermission>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<ApiKeyBucketPermission>()
            .HasOne(p => p.ApiKey)
            .WithMany()
            .HasForeignKey(p => p.ApiKeyId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApiKeyBucketPermission>()
            .HasOne(p => p.Bucket)
            .WithMany()
            .HasForeignKey(p => p.BucketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Document>()
            .HasOne(d => d.Bucket)
            .WithMany(b => b.Documents)
            .HasForeignKey(d => d.BucketId)
            .OnDelete(DeleteBehavior.SetNull);

        // Performance indexes for Document
        modelBuilder.Entity<Document>()
            .HasIndex(d => new { d.CreatedAt, d.IsDeleted })
            .HasDatabaseName("IX_Documents_CreatedAt_IsDeleted");

        modelBuilder.Entity<Document>()
            .HasIndex(d => new { d.StorageProviderId, d.IsDeleted })
            .HasDatabaseName("IX_Documents_ProviderId_IsDeleted");

        modelBuilder.Entity<Document>()
            .HasIndex(d => new { d.BucketId, d.IsDeleted })
            .HasDatabaseName("IX_Documents_BucketId_IsDeleted");

        modelBuilder.Entity<DocumentShare>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<DocumentShare>()
            .HasIndex(e => e.ShareCode)
            .IsUnique();

        modelBuilder.Entity<DocumentShare>()
            .HasIndex(e => e.DocumentId);

        modelBuilder.Entity<DocumentShare>()
            .HasOne(e => e.Document)
            .WithMany()
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // DocumentShare CreatedBy navigation is configured globally via BaseEntity

        modelBuilder.Entity<DocumentShareView>()
            .HasKey(e => e.Id);

        modelBuilder.Entity<DocumentShareView>()
            .HasOne(e => e.Share)
            .WithMany(s => s.Views)
            .HasForeignKey(e => e.ShareId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DocumentShare>()
            .HasOne(s => s.CreatedViaApiKey)
            .WithMany()
            .HasForeignKey(s => s.CreatedViaApiKeyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApprovalSettings>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<ApprovalSettings>()
            .HasData(new ApprovalSettings
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                IsGlobalApprovalEnabled = false,
                DefaultExpirationDays = 7,
                DefaultRequiredApprovals = 1,
                ForceApprovalForAll = false,
                ForceApprovalForLargeFiles = true,
                LargeFileSizeThresholdBytes = 100 * 1024 * 1024,
                EnableEmailNotifications = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<ApprovalSettings>()
            .HasOne(s => s.GlobalApprovalEnabledByUser)
            .WithMany()
            .HasForeignKey(s => s.GlobalApprovalEnabledByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ApprovalPolicy>()
            .HasKey(p => p.Id);

        modelBuilder.Entity<ApprovalPolicy>()
            .HasIndex(p => p.Name);

        // ApprovalPolicy CreatedBy navigation is configured globally via BaseEntity

        modelBuilder.Entity<ApprovalPolicy>()
            .HasData(new ApprovalPolicy
            {
                Id = Guid.Parse("00000000-0000-0000-0002-000000000001"),
                Name = "Global System Policy",
                Description =
                    "Core system policy that handles all approval requirements. This policy cannot be deleted and is automatically applied when specific policies don't match.",
                IsActive = true,
                Priority = 999,
                RequireApproval = true,
                ApprovalTimeoutHours = 72,
                CreatedBy = null,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

        modelBuilder.Entity<ShareApprovalRequest>()
            .HasKey(r => r.Id);

        modelBuilder.Entity<ShareApprovalRequest>()
            .HasIndex(r => r.DocumentShareId);

        modelBuilder.Entity<ShareApprovalRequest>()
            .HasIndex(r => r.Status);

        modelBuilder.Entity<ShareApprovalRequest>()
            .HasIndex(r => r.ExpiresAt);

        modelBuilder.Entity<ShareApprovalRequest>()
            .HasOne(r => r.DocumentShare)
            .WithMany(s => s.ApprovalRequests)
            .HasForeignKey(r => r.DocumentShareId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShareApprovalRequest>()
            .HasOne(r => r.ApprovalPolicy)
            .WithMany(p => p.ApprovalRequests)
            .HasForeignKey(r => r.ApprovalPolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ShareApprovalRequest>()
            .HasOne(r => r.RequestedByUser)
            .WithMany()
            .HasForeignKey(r => r.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApprovalDecision>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<ApprovalDecision>()
            .HasIndex(d => d.ShareApprovalRequestId);

        modelBuilder.Entity<ApprovalDecision>()
            .HasIndex(d => d.ApproverUserId);

        modelBuilder.Entity<ApprovalDecision>()
            .HasOne(d => d.ShareApprovalRequest)
            .WithMany(r => r.ApprovalDecisions)
            .HasForeignKey(d => d.ShareApprovalRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApprovalDecision>()
            .HasOne(d => d.ApproverUser)
            .WithMany()
            .HasForeignKey(d => d.ApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ApprovalHistory>()
            .HasKey(h => h.Id);

        modelBuilder.Entity<ApprovalHistory>()
            .HasIndex(h => h.ShareApprovalRequestId);

        modelBuilder.Entity<ApprovalHistory>()
            .HasIndex(h => h.ActionByUserId);

        modelBuilder.Entity<ApprovalHistory>()
            .HasIndex(h => h.ActionDate);

        modelBuilder.Entity<ApprovalHistory>()
            .HasOne(h => h.ShareApprovalRequest)
            .WithMany(r => r.ApprovalHistory)
            .HasForeignKey(h => h.ShareApprovalRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApprovalHistory>()
            .HasOne(h => h.ActionByUser)
            .WithMany()
            .HasForeignKey(h => h.ActionByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // EmailSettings configuration
        modelBuilder.Entity<EmailSettings>()
            .HasKey(e => e.Id);

        // EmailTemplate configuration
        modelBuilder.Entity<EmailTemplate>()
            .HasKey(t => t.Id);

        modelBuilder.Entity<EmailTemplate>()
            .HasIndex(t => t.TemplateType)
            .IsUnique();
    }

    /// <summary>
    /// This method is deprecated. Storage directories are now created during initial setup.
    /// </summary>
    [Obsolete("Storage directories are now created during initial setup, not as seed data")]
    public void EnsureSeedDirectoriesCreated()
    {
        // No-op - directories will be created during initial setup
    }
}
