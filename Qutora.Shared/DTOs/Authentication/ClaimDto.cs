namespace Qutora.Shared.DTOs.Authentication;

/// <summary>
/// Yetki talepleri (claim) için veri transfer nesnesi
/// </summary>
public class ClaimDto
{
    /// <summary>
    /// Talep tipi
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Talep değeri
    /// </summary>
    public string Value { get; set; } = string.Empty;
}