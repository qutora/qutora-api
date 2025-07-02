using System.Text.Json;

namespace Qutora.Infrastructure.Storage.Models;

/// <summary>
/// Configuration class for FileSystem provider
/// </summary>
public class FileSystemProviderConfig : IProviderConfig
{
    /// <summary>
    /// Provider unique identifier
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Provider type
    /// </summary>
    public string ProviderType => "filesystem";

    /// <summary>
    /// Root directory (base) path
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>
    /// Should directory be created if it doesn't exist?
    /// </summary>
    public bool CreateDirectoryIfNotExists { get; set; } = true;

    /// <summary>
    /// Converts config object to JSON
    /// </summary>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this);
    }

    /// <summary>
    /// Creates config object from JSON data
    /// </summary>
    public static IProviderConfig? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new FileSystemProviderConfig();

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var config = JsonSerializer.Deserialize<FileSystemProviderConfig>(json, options);

            var jsonDoc = JsonDocument.Parse(json);
            if (jsonDoc.RootElement.TryGetProperty("createIfNotExists", out var createIfNotExistsElement))
            {
                switch (createIfNotExistsElement.ValueKind)
                {
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        config.CreateDirectoryIfNotExists = createIfNotExistsElement.GetBoolean();
                        break;
                    case JsonValueKind.String:
                        bool.TryParse(createIfNotExistsElement.GetString(), out var boolValue);
                        config.CreateDirectoryIfNotExists = boolValue;
                        break;
                }
            }

            return config;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"FileSystemProviderConfig JSON parsing error: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Converts to FileSystemProviderOptions object
    /// </summary>
    public object ToOptions()
    {
        return new FileSystemProviderOptions
        {
            RootPath = RootPath,
            CreateDirectoryIfNotExists = CreateDirectoryIfNotExists
        };
    }
}
