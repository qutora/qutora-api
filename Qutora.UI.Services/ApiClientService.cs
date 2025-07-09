using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Qutora.UI.Services.Interfaces;
using Qutora.Shared.DTOs.Common;

namespace Qutora.UI.Services;

/// <summary>
/// HTTP API client service
/// </summary>
public class ApiClientService : IApiClientService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiClientService(
        HttpClient httpClient,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

        var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? throw new InvalidOperationException("ApiSettings:BaseUrl configuration is required");
        _httpClient.BaseAddress = new Uri(baseUrl);
        _httpClient.DefaultRequestHeaders.Accept.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Add client tracking headers to the request
    /// </summary>
    private void AddClientHeaders(HttpRequestMessage request)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Get real client IP (priority: X-Forwarded-For > X-Real-IP > RemoteIpAddress)
            var clientIp = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                          ?? httpContext.Request.Headers["X-Real-IP"].FirstOrDefault()
                          ?? httpContext.Connection.RemoteIpAddress?.ToString()
                          ?? "unknown";

            // Get user agent
            var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown";

            // Add headers to API request
            request.Headers.Add("X-Client-IP", clientIp);
            request.Headers.Add("X-User-Agent", userAgent);
        }
    }

    public async Task<T> GetAsync<T>(string endpoint, bool requiresAuth = true)
    {
        // URL oluşturulurken, endpoint başında / varsa temizle
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        // URL'yi oluştur
        var url = new Uri(_httpClient.BaseAddress, endpoint).ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        
        // Add client tracking headers
        AddClientHeaders(request);
        
        return await SendRequestAsync<T>(request, requiresAuth);
    }

    public async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> queryParams, bool requiresAuth = true)
    {
        // URL oluşturulurken, endpoint başında / varsa temizle
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        // URL'yi oluştur
        var url = new Uri(_httpClient.BaseAddress, endpoint).ToString();
        var uriBuilder = new UriBuilder(url);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);

        // Sorgu parametrelerini ekle
        if (queryParams != null)
            foreach (var param in queryParams)
                query[param.Key] = param.Value;

        uriBuilder.Query = query.ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
        
        // Add client tracking headers
        AddClientHeaders(request);
        
        return await SendRequestAsync<T>(request, requiresAuth);
    }

    public async Task<T> PostAsync<T>(string endpoint, object data, bool requiresAuth = true)
    {
        // URL oluşturulurken, endpoint başında / varsa temizle
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        // URL'yi oluştur
        var url = new Uri(_httpClient.BaseAddress, endpoint).ToString();
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (data != null)
            request.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        
        // Add client tracking headers
        AddClientHeaders(request);
        
        return await SendRequestAsync<T>(request, requiresAuth);
    }

    public async Task<T> PostFormAsync<T>(string endpoint, MultipartFormDataContent formData, bool requiresAuth = true)
    {
        // URL oluşturulurken, endpoint başında / varsa temizle
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        // URL'yi oluştur
        var url = new Uri(_httpClient.BaseAddress, endpoint).ToString();
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = formData;
        
        // Add client tracking headers
        AddClientHeaders(request);
        
        return await SendRequestAsync<T>(request, requiresAuth);
    }

    public async Task<T> PutAsync<T>(string endpoint, object data, bool requiresAuth = true)
    {
        // URL oluşturulurken, endpoint başında / varsa temizle
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        // URL'yi oluştur
        var url = new Uri(_httpClient.BaseAddress, endpoint).ToString();
        var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        
        // Add client tracking headers
        AddClientHeaders(request);
        
        return await SendRequestAsync<T>(request, requiresAuth);
    }

    public async Task<T> PatchAsync<T>(string endpoint, object data, bool requiresAuth = true)
    {
        // URL oluşturulurken, endpoint başında / varsa temizle
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        // URL'yi oluştur
        var url = new Uri(_httpClient.BaseAddress, endpoint).ToString();
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url);
        if (data != null)
            request.Content = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
        
        // Add client tracking headers
        AddClientHeaders(request);
        
        return await SendRequestAsync<T>(request, requiresAuth);
    }

    public async Task<T> DeleteAsync<T>(string endpoint, bool requiresAuth = true)
    {
        // URL oluşturulurken, endpoint başında / varsa temizle
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        // URL'yi oluştur
        var url = new Uri(_httpClient.BaseAddress, endpoint).ToString();
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        
        // Add client tracking headers
        AddClientHeaders(request);
        
        return await SendRequestAsync<T>(request, requiresAuth);
    }

    public async Task<T> DeleteAsync<T>(string endpoint, Dictionary<string, string> queryParams,
        bool requiresAuth = true)
    {
        // URL oluşturulurken, endpoint başında / varsa temizle
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        // URL'yi oluştur
        var url = new Uri(_httpClient.BaseAddress, endpoint).ToString();
        var uriBuilder = new UriBuilder(url);
        var query = System.Web.HttpUtility.ParseQueryString(string.Empty);

        // Sorgu parametrelerini ekle
        if (queryParams != null)
            foreach (var param in queryParams)
                query[param.Key] = param.Value;

        uriBuilder.Query = query.ToString();
        var request = new HttpRequestMessage(HttpMethod.Delete, uriBuilder.Uri);
        
        // Add client tracking headers
        AddClientHeaders(request);
        
        return await SendRequestAsync<T>(request, requiresAuth);
    }

    public async Task<Stream> GetStreamAsync(string endpoint, bool requiresAuth = true)
    {
        if (requiresAuth)
        {
            // ✅ GÜVENLİ ÇÖZÜM: HttpOnly cookie'den JWT token oku
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["QutoraToken"];
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("Kimlik doğrulama gerekli. Lütfen giriş yapın.");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // URL oluşturulurken, endpoint başında / varsa temizle
        if (endpoint.StartsWith("/"))
            endpoint = endpoint.Substring(1);

        // URL'yi oluştur
        var url = new Uri(_httpClient.BaseAddress, endpoint).ToString();

        // Create request and add client headers
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        AddClientHeaders(request);

        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        if (response.IsSuccessStatusCode) return await response.Content.ReadAsStreamAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Kimlik doğrulama başarısız oldu. Lütfen tekrar giriş yapın.");

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Bu işlem için yetkiniz bulunmuyor. Sistem yöneticisine başvurun.");

        var errorContent = await response.Content.ReadAsStringAsync();
        string errorMessage = errorContent;
        
        if (!string.IsNullOrEmpty(errorContent))
        {
            try
            {
                var messageResponse = JsonSerializer.Deserialize<MessageResponseDto>(errorContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                });
                
                if (messageResponse != null && !string.IsNullOrEmpty(messageResponse.Message))
                {
                    errorMessage = messageResponse.Message;
                }
            }
            catch (JsonException)
            {
                // errorContent'i zaten kullanıyoruz
            }
        }

        throw new HttpRequestException(errorMessage);
    }

    private async Task<T> SendRequestAsync<T>(HttpRequestMessage request, bool requiresAuth)
    {
        if (requiresAuth)
        {
            // ✅ GÜVENLİ ÇÖZÜM: HttpOnly cookie'den JWT token oku
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["QutoraToken"];
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException("Kimlik doğrulama gerekli. Lütfen giriş yapın.");
            
            // JWT Bearer token'ı authorization header'a ekle
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            // 204 NoContent yanıtı için özel işlem
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                // bool veya object tipinde ise, başarı olarak true dön
                if (typeof(T) == typeof(bool))
                    return (T)(object)true;
                else if (typeof(T) == typeof(object)) return default;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();

            // Boş içerik kontrolü
            if (string.IsNullOrEmpty(jsonResponse) || jsonResponse == "{}" || jsonResponse == "[]") return default;

            try
            {
                // Referans takibi destekli deserializasyon ayarları
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                };

                // Normal deserializasyon
                return JsonSerializer.Deserialize<T>(jsonResponse, options);
            }
            catch (JsonException)
            {
                throw;
            }
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Kimlik doğrulama başarısız oldu. Lütfen tekrar giriş yapın.");

        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("Bu işlem için yetkiniz bulunmuyor. Sistem yöneticisine başvurun.");

        var errorContent = await response.Content.ReadAsStringAsync();
        string errorMessage = errorContent;
        
        if (!string.IsNullOrEmpty(errorContent))
        {
            try
            {
                var messageResponse = JsonSerializer.Deserialize<MessageResponseDto>(errorContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = ReferenceHandler.Preserve
                });
                
                if (messageResponse != null && !string.IsNullOrEmpty(messageResponse.Message))
                {
                    errorMessage = messageResponse.Message;
                }
            }
            catch (JsonException)
            {
                // errorContent'i zaten kullanıyoruz
            }
        }

        throw new HttpRequestException(errorMessage);
    }
} 