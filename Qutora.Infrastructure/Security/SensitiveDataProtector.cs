using Microsoft.AspNetCore.DataProtection;
using System.Text;
using System.Text.Json;

namespace Qutora.Infrastructure.Security;

/// <summary>
/// Interface for protecting sensitive data
/// </summary>
public interface ISensitiveDataProtector
{
    /// <summary>
    /// Encrypts the given plain text
    /// </summary>
    /// <param name="plaintext">Plain text to encrypt</param>
    /// <returns>Encrypted text</returns>
    string Protect(string plaintext);

    /// <summary>
    /// Decrypts the encrypted text
    /// </summary>
    /// <param name="protectedData">Encrypted text to decrypt</param>
    /// <returns>Decrypted plain text</returns>
    string Unprotect(string protectedData);

    /// <summary>
    /// Checks if the data is encrypted
    /// </summary>
    /// <param name="data">Data to check</param>
    /// <returns>True if data is encrypted, false otherwise</returns>
    bool IsProtected(string data);

    /// <summary>
    /// Determines sensitive config keys by provider type
    /// </summary>
    /// <param name="providerType">Provider type</param>
    /// <returns>Sensitive field names</returns>
    string[] GetSensitiveConfigKeys(string providerType);

    /// <summary>
    /// Encrypts sensitive fields in ConfigJson
    /// </summary>
    /// <param name="configJson">Original config JSON</param>
    /// <param name="providerType">Provider type</param>
    /// <returns>JSON with encrypted sensitive fields</returns>
    string ProtectSensitiveConfigJson(string configJson, string providerType);

    /// <summary>
    /// Decrypts encrypted sensitive fields in ConfigJson
    /// </summary>
    /// <param name="configJson">Encrypted config JSON</param>
    /// <param name="providerType">Provider type</param>
    /// <returns>JSON with decrypted sensitive fields</returns>
    string UnprotectSensitiveConfigJson(string configJson, string providerType);
}

/// <summary>
/// Class that encrypts and decrypts sensitive data using ASP.NET Core Data Protection API
/// </summary>
public class SensitiveDataProtector(IDataProtectionProvider provider) : ISensitiveDataProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector("Qutora.StorageProviders.SensitiveData");
    private const string ProtectedPrefix = "PROTECTED:";

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;
        if (IsProtected(plaintext)) return plaintext;

        var protectedBytes = _protector.Protect(Encoding.UTF8.GetBytes(plaintext));
        return ProtectedPrefix + Convert.ToBase64String(protectedBytes);
    }

    public string Unprotect(string protectedData)
    {
        if (string.IsNullOrEmpty(protectedData)) return protectedData;
        if (!IsProtected(protectedData)) return protectedData;

        var actualProtectedData = protectedData.Substring(ProtectedPrefix.Length);
        var protectedBytes = Convert.FromBase64String(actualProtectedData);
        var plaintextBytes = _protector.Unprotect(protectedBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }

    public bool IsProtected(string data)
    {
        return !string.IsNullOrEmpty(data) && data.StartsWith(ProtectedPrefix);
    }

    /// <summary>
    /// Determines sensitive config keys by provider type
    /// </summary>
    public string[] GetSensitiveConfigKeys(string providerType)
    {
        return providerType.ToLowerInvariant() switch
        {
            "minio" => ["accessKey", "secretKey"],
            "ftp" => ["password"],
            "sftp" => ["password", "privateKey", "privateKeyPassphrase"],
            _ => []
        };
    }

    /// <summary>
    /// Encrypts sensitive fields in ConfigJson
    /// </summary>
    public string ProtectSensitiveConfigJson(string configJson, string providerType)
    {
        if (string.IsNullOrEmpty(configJson))
            return configJson;

        try
        {
            var jsonDoc = JsonDocument.Parse(configJson);
            var configDict = new Dictionary<string, object?>();

            foreach (var property in jsonDoc.RootElement.EnumerateObject())
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        configDict[property.Name] = property.Value.GetString() ?? string.Empty;
                        break;
                    case JsonValueKind.Number when property.Value.TryGetInt32(out var intValue):
                        configDict[property.Name] = intValue;
                        break;
                    case JsonValueKind.Number:
                        configDict[property.Name] = property.Value.GetDouble();
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        configDict[property.Name] = property.Value.GetBoolean();
                        break;
                    case JsonValueKind.Null:
                        configDict[property.Name] = null;
                        break;
                    default:
                        configDict[property.Name] = property.Value.ToString();
                        break;
                }

            var sensitiveKeys = GetSensitiveConfigKeys(providerType);

            foreach (var key in sensitiveKeys)
                if (configDict.ContainsKey(key) &&
                    configDict[key] != null &&
                    configDict[key]?.ToString() != "" &&
                    !IsProtected(configDict[key]?.ToString() ?? ""))
                {
                    configDict[key] = Protect(configDict[key]?.ToString() ?? "");
                }

            return JsonSerializer.Serialize(configDict);
        }
        catch (Exception)
        {
        }

        return configJson;
    }

    /// <summary>
    /// Decrypts encrypted sensitive fields in ConfigJson
    /// </summary>
    public string UnprotectSensitiveConfigJson(string configJson, string providerType)
    {
        if (string.IsNullOrEmpty(configJson))
            return configJson;

        try
        {
            var jsonDoc = JsonDocument.Parse(configJson);
            var configDict = new Dictionary<string, object?>();

            foreach (var property in jsonDoc.RootElement.EnumerateObject())
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                        configDict[property.Name] = property.Value.GetString() ?? string.Empty;
                        break;
                    case JsonValueKind.Number when property.Value.TryGetInt32(out var intValue):
                        configDict[property.Name] = intValue;
                        break;
                    case JsonValueKind.Number:
                        configDict[property.Name] = property.Value.GetDouble();
                        break;
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        configDict[property.Name] = property.Value.GetBoolean();
                        break;
                    case JsonValueKind.Null:
                        configDict[property.Name] = null;
                        break;
                    default:
                        configDict[property.Name] = property.Value.ToString();
                        break;
                }

            var sensitiveKeys = GetSensitiveConfigKeys(providerType);

            foreach (var key in sensitiveKeys)
                if (configDict.ContainsKey(key) &&
                    configDict[key] != null &&
                    configDict[key]?.ToString() != "" &&
                    IsProtected(configDict[key]?.ToString() ?? ""))
                {
                    configDict[key] = Unprotect(configDict[key]?.ToString() ?? "");
                }

            return JsonSerializer.Serialize(configDict);
        }
        catch (Exception)
        {
        }

        return configJson;
    }
}
