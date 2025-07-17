namespace Qutora.Application.Interfaces;

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