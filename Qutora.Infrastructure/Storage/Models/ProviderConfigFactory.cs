using System.Text.Json;

namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// Factory class for creating provider config objects
/// </summary>
public static class ProviderConfigFactory
{
    /// <summary>
    /// Creates an empty config object based on provider type
    /// </summary>
    public static IProviderConfig? Create(string providerType)
    {
        return providerType.ToLower() switch
        {
            "filesystem" => new FileSystemProviderConfig { ProviderId = Guid.NewGuid().ToString() },
            "minio" => new MinioProviderConfig { ProviderId = Guid.NewGuid().ToString() },
            "s3" => new MinioProviderConfig { ProviderId = Guid.NewGuid().ToString() },
            "ftp" => new FtpProviderConfig { ProviderId = Guid.NewGuid().ToString() },
            "sftp" => new SftpProviderConfig { ProviderId = Guid.NewGuid().ToString() },
            _ => throw new ArgumentException($"Unsupported provider type: {providerType}")
        };
    }

    /// <summary>
    /// Creates config object from JSON data
    /// </summary>
    public static IProviderConfig? FromJson(string providerType, string json)
    {
        if (string.IsNullOrEmpty(json))
            return Create(providerType);

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

            var jsonDoc = JsonDocument.Parse(json);
            var configDict = new Dictionary<string, object>();

            foreach (var property in jsonDoc.RootElement.EnumerateObject())
                switch (property.Value.ValueKind)
                {
                    case JsonValueKind.String:
                    {
                        var stringValue = property.Value.GetString() ?? string.Empty;

                        if (property.Name.Equals("useSSL", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("forcePathStyle", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("createBucketIfNotExists", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("createIfNotExists", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("isDefault", StringComparison.OrdinalIgnoreCase) ||
                            property.Name.Equals("isActive", StringComparison.OrdinalIgnoreCase))
                        {
                            if (bool.TryParse(stringValue, out var boolValue))
                                configDict[property.Name] = boolValue;
                            else
                                configDict[property.Name] = false;
                        }
                        else if (property.Name.Equals("port", StringComparison.OrdinalIgnoreCase) ||
                                 property.Name.Equals("timeout", StringComparison.OrdinalIgnoreCase) ||
                                 property.Name.Equals("maxConnections", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(stringValue, out var intValue))
                                configDict[property.Name] = intValue;
                            else
                                configDict[property.Name] = 0;
                        }
                        else
                        {
                            configDict[property.Name] = stringValue;
                        }

                        break;
                    }
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

            var normalizedJson = JsonSerializer.Serialize(configDict, options);

            IProviderConfig? result = providerType.ToLower() switch
            {
                "filesystem" => JsonSerializer.Deserialize<FileSystemProviderConfig>(normalizedJson, options),
                "minio" => JsonSerializer.Deserialize<MinioProviderConfig>(normalizedJson, options),
                "s3" => JsonSerializer.Deserialize<MinioProviderConfig>(normalizedJson,
                    options),
                "ftp" => JsonSerializer.Deserialize<FtpProviderConfig>(normalizedJson, options),
                "sftp" => JsonSerializer.Deserialize<SftpProviderConfig>(normalizedJson, options),
                _ => throw new ArgumentException($"Unsupported provider type: {providerType}")
            };

            return result;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Config JSON parsing error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Returns schema dictionary based on provider type
    /// </summary>
    public static Dictionary<string, object> GetSchema(string providerType)
    {
        var config = Create(providerType);
        if (config == null)
            return new Dictionary<string, object>();

        var properties = config.GetType().GetProperties();
        var schema = new Dictionary<string, object>();

        foreach (var prop in properties)
        {
            if (prop.DeclaringType == typeof(IProviderConfig) || !prop.CanWrite)
                continue;

            var propSchema = new Dictionary<string, object>();

            if (prop.PropertyType == typeof(bool))
            {
                propSchema["type"] = "boolean";
            }
            else if (prop.PropertyType == typeof(int))
            {
                propSchema["type"] = "number";
            }
            else if (prop.PropertyType == typeof(string))
            {
                propSchema["type"] = "text";

                if (prop.Name.Contains("Password") || prop.Name.Contains("SecretKey"))
                    propSchema["type"] = "password";
                else if (prop.Name.Contains("Endpoint")) propSchema["type"] = "url";
            }

            var isRequired = prop
                .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute), false).Any();
            if (isRequired) propSchema["required"] = true;

            schema[prop.Name] = propSchema;
        }

        return schema;
    }
}
