namespace Qutora.Application.Models.Validation;

/// <summary>
/// Metadata doğrulama hatası
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Hata olan alan
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// Hata mesajı
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Yeni doğrulama hatası oluşturur
    /// </summary>
    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }
}