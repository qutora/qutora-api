using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Qutora.Infrastructure.Security;

public static class DataProtectionExtensions
{
    public static IServiceCollection AddQutoraDataProtection(
        this IServiceCollection services,
        string contentRootPath,
        ILogger logger)
    {
        var keysDirectory = new DirectoryInfo(Path.Combine(contentRootPath, "keys"));
        
        // Smart detection of key source
        var keySource = DetectKeySource(keysDirectory, logger);
        
        // Setup data protection based on detected source
        var dataProtectionBuilder = services.AddDataProtection()
            .SetApplicationName("Qutora.DocumentSharing");
            
        switch (keySource.Type)
        {
            case KeySourceType.ExternalVolume:
                logger.LogInformation("KEY: Using external keys from mounted volume: {KeysPath}", keysDirectory.FullName);
                dataProtectionBuilder.PersistKeysToFileSystem(keysDirectory);
                break;
                
            case KeySourceType.InternalGeneration:
                // CRITICAL WARNING for production usage
                logger.LogWarning("***  CRITICAL DATA PROTECTION WARNING  ***");
                logger.LogWarning("** Using INTERNAL key generation - keys will be LOST when container is removed!");
                logger.LogWarning("** This will make ALL encrypted data UNRECOVERABLE!");
                logger.LogWarning("** For production, use: docker run -v qutora_keys:/app/keys");
                logger.LogWarning("** Documentation: https://qutora.io/docs/key-management");
                logger.LogWarning("*************************************************");
                
                // Ensure directory exists for internal generation
                if (!keysDirectory.Exists)
                {
                    keysDirectory.Create();
                    // Set secure permissions (Linux/Mac)
                    if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                    {
                        try
                        {
                            File.SetUnixFileMode(keysDirectory.FullName, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Could not set Unix permissions on keys directory");
                        }
                    }
                }
                logger.LogInformation("KEY: Using internal key generation: {KeysPath}", keysDirectory.FullName);
                
                // Add warning to logs every 5 minutes in production
                if (System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
                {
                    logger.LogError("** PRODUCTION ALERT: Internal key generation detected in PRODUCTION environment!");
                    logger.LogError("** This is NOT recommended for production! Use persistent volume for keys!");
                }
                
                dataProtectionBuilder.PersistKeysToFileSystem(keysDirectory);
                break;
                
            case KeySourceType.ReadOnlyMount:
                logger.LogInformation("KEY: Using read-only mounted keys: {KeysPath}", keysDirectory.FullName);
                dataProtectionBuilder.PersistKeysToFileSystem(keysDirectory);
                break;
        }
        
        // Add key rotation policy
        dataProtectionBuilder.SetDefaultKeyLifetime(TimeSpan.FromDays(90));
        
        // Add warning service for internal key generation
        services.AddDataProtectionWarning(keysDirectory.FullName, keySource.Type);
        
        return services;
    }
    
    private static KeySourceInfo DetectKeySource(DirectoryInfo keysDirectory, ILogger logger)
    {
        if (!keysDirectory.Exists)
        {
            logger.LogDebug("Keys directory does not exist, will use internal generation");
            return new KeySourceInfo(KeySourceType.InternalGeneration, "Directory does not exist");
        }
        
        var keyFiles = keysDirectory.GetFiles("*.xml");
        
        if (!keyFiles.Any())
        {
            logger.LogDebug("Keys directory exists but is empty, will use internal generation");
            return new KeySourceInfo(KeySourceType.InternalGeneration, "Directory is empty");
        }
        
        // Check if directory is read-only (K8s secret mount)
        try
        {
            var testFile = Path.Combine(keysDirectory.FullName, $"test-{Guid.NewGuid()}.tmp");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            
            // If we can write, check if keys look externally managed
            if (HasExternalKeyCharacteristics(keyFiles, logger))
            {
                logger.LogDebug("Keys directory has external key characteristics");
                return new KeySourceInfo(KeySourceType.ExternalVolume, "External keys detected");
            }
            else
            {
                logger.LogDebug("Keys directory appears to be internally managed");
                return new KeySourceInfo(KeySourceType.InternalGeneration, "Internal keys detected");
            }
        }
        catch (UnauthorizedAccessException)
        {
            logger.LogDebug("Keys directory is read-only, likely a mounted secret");
            return new KeySourceInfo(KeySourceType.ReadOnlyMount, "Read-only mount detected");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not determine key source, defaulting to internal generation");
            return new KeySourceInfo(KeySourceType.InternalGeneration, "Detection failed");
        }
    }
    
    private static bool HasExternalKeyCharacteristics(FileInfo[] keyFiles, ILogger logger)
    {
        try
        {
            // Check for characteristics that suggest external management:
            // 1. Multiple keys with similar creation times (bulk import)
            // 2. Keys created before application start time
            // 3. Specific naming patterns
            
            var now = DateTime.UtcNow;
            var appStartThreshold = now.AddMinutes(-5); // App likely started within last 5 minutes
            
            var oldKeys = keyFiles.Where(f => f.CreationTimeUtc < appStartThreshold).ToArray();
            
            if (oldKeys.Length > 0)
            {
                logger.LogDebug("Found {Count} keys created before application start", oldKeys.Length);
                return true;
            }
            
            // Check for bulk creation (multiple keys created within seconds)
            if (keyFiles.Length > 1)
            {
                var creationTimes = keyFiles.Select(f => f.CreationTimeUtc).OrderBy(t => t).ToArray();
                var maxTimeDiff = creationTimes.Last() - creationTimes.First();
                
                if (maxTimeDiff.TotalSeconds < 10) // All created within 10 seconds
                {
                    logger.LogDebug("Found bulk key creation pattern, likely external");
                    return true;
                }
            }
            
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error analyzing key characteristics");
            return false;
        }
    }
}