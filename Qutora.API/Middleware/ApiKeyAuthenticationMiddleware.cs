namespace Qutora.API.Middleware;

/// <summary>
/// API Key authentication middleware
/// Bu middleware, API key header'larını kontrol eder ve authentication scheme yönlendirmesi yapar
/// </summary>
public class ApiKeyAuthenticationMiddleware(RequestDelegate next)
{
    private const string API_KEY_HEADER = "X-QUTORA-Key";
    private const string API_SECRET_HEADER = "X-QUTORA-Secret";

    public async Task InvokeAsync(HttpContext context)
    {
        // API key header'ları varsa, authentication scheme'i belirlemeye yardımcı ol
        var hasApiKeyHeaders = context.Request.Headers.ContainsKey(API_KEY_HEADER) && 
                              context.Request.Headers.ContainsKey(API_SECRET_HEADER);

        var hasAuthorizationHeader = context.Request.Headers.ContainsKey("Authorization");

        // Eğer hem API key hem Authorization header varsa, API key'i öncelikli tut
        if (hasApiKeyHeaders && hasAuthorizationHeader)
        {
            // Authorization header'ını geçici olarak kaldır ki JWT authentication handler çalışmasın
            context.Items["OriginalAuthHeader"] = context.Request.Headers["Authorization"].ToString();
            context.Request.Headers.Remove("Authorization");
        }

        // Eğer sadece API key varsa, o zaman ApiKey scheme kullanılacak
        // Eğer sadece Authorization header varsa, o zaman JWT Bearer scheme kullanılacak
        // Authentication handler'lar bunu otomatik olarak idare eder

        await next(context);

        // Eğer Authorization header'ını kaldırdıysak, geri koy (response için gerekli olabilir)
        if (context.Items.ContainsKey("OriginalAuthHeader"))
        {
            context.Request.Headers["Authorization"] = context.Items["OriginalAuthHeader"]?.ToString();
            context.Items.Remove("OriginalAuthHeader");
        }
    }
}
