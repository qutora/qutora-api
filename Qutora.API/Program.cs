using Qutora.Infrastructure.Logging;
using Qutora.Infrastructure.Security;
using Serilog;
using Qutora.Shared.DTOs;
using Qutora.Shared.Models;
using Qutora.API.Extensions;
using Qutora.API.Middleware;
using Qutora.Application.Interfaces;
using Qutora.Application.Security;
using Qutora.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

var logger = SerilogLogger.ConfigureLogger("Qutora.API", builder.Environment.EnvironmentName);
builder.Host.UseSerilog(logger);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "Database connection string is required! Set ConnectionStrings__DefaultConnection environment variable.");
logger.Information("✅ Database: {DatabaseInfo}", MaskConnectionString(connectionString));

var databaseProvider = builder.Configuration["Database:Provider"];
if (string.IsNullOrWhiteSpace(databaseProvider))
    throw new InvalidOperationException(
        "Database provider is required! Set Database__Provider environment variable (SqlServer/PostgreSQL/MySQL).");
logger.Information("✅ Database Provider: {Provider}", databaseProvider);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "JWT Key is required! Set Jwt__Key environment variable.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"];
if (string.IsNullOrWhiteSpace(jwtIssuer))
    throw new InvalidOperationException(
        "JWT Issuer is required! Set Jwt__Issuer environment variable.");

var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtAudience))
    throw new InvalidOperationException(
        "JWT Audience is required! Set Jwt__Audience environment variable.");

logger.Information("✅ JWT Issuer: {Issuer}", jwtIssuer);
logger.Information("✅ JWT Audience: {Audience}", jwtAudience);

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
if (allowedOrigins == null || allowedOrigins.Length == 0)
    throw new InvalidOperationException(
        "CORS origins are required! Set AllowedOrigins__0, AllowedOrigins__1, etc. environment variables.");
logger.Information("✅ CORS Allowed Origins: {Origins}", string.Join(", ", allowedOrigins));

var publicViewerOrigins = builder.Configuration.GetSection("PublicViewerOrigins").Get<string[]>();
if (publicViewerOrigins == null || publicViewerOrigins.Length == 0)
{
    logger.Information(
        "ℹ️ Public Viewer CORS: No origins configured. Set PublicViewerOrigins__0, PublicViewerOrigins__1, etc. to enable Public Document Viewer");
    publicViewerOrigins = [];
}
else
{
    logger.Information("✅ Public Viewer CORS Origins: {Origins}", string.Join(", ", publicViewerOrigins));
}

// PublicViewer Settings
var publicViewerBaseUrl = builder.Configuration["PublicViewer:BaseUrl"];
if (string.IsNullOrWhiteSpace(publicViewerBaseUrl))
{
    logger.Warning("⚠️ PublicViewer BaseUrl not configured. Email links may not work properly. Set PublicViewer__BaseUrl environment variable.");
}
else
{
    logger.Information("✅ PublicViewer BaseUrl: {BaseUrl}", publicViewerBaseUrl);
}

builder.Services.Configure<PublicViewerSettings>(options =>
{
    options.BaseUrl = publicViewerBaseUrl ?? string.Empty;
});

// Email sample data configuration
builder.Services.Configure<EmailSampleDataSettings>(
    builder.Configuration.GetSection("EmailSampleData"));

// Smart Data Protection key management (supports Docker volumes, K8s secrets, internal generation)
using (var tempProvider = builder.Services.BuildServiceProvider())
{
    var msLogger = tempProvider.GetRequiredService<ILogger<Program>>();
    builder.Services.AddQutoraDataProtection(builder.Environment.ContentRootPath, msLogger);
}

builder.Services.AddSingleton<ISensitiveDataProtector, SensitiveDataProtector>();

builder.Services.AddMemoryCache();

builder.Services.AddDatabaseServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddStorageServices(builder.Configuration);
builder.Services.AddSecurityServices(builder.Configuration);
builder.Services.AddApiServices();
builder.Services.AddPublicViewerAuth(builder.Configuration);
// API services

builder.Services.AddCors(options =>
{
    var allOrigins = new List<string>(allowedOrigins);
    if (publicViewerOrigins.Length > 0) allOrigins.AddRange(publicViewerOrigins);

    options.AddPolicy("AllowedOrigins", policyBuilder =>
    {
        policyBuilder.WithOrigins(allOrigins.ToArray())
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });

    logger.Information("✅ Combined CORS Origins: {Origins}", string.Join(", ", allOrigins));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    
    // Retry logic for database initialization (especially for Docker containers)
    var maxRetries = 10;
    var retryDelay = TimeSpan.FromSeconds(5);
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.Information("Database initialization attempt {Attempt}/{MaxRetries}", attempt, maxRetries);
            
            var initializer = services.GetRequiredService<ApplicationDbContextInitializer>();
            initializer.InitializeAsync().GetAwaiter().GetResult();
            logger.Information("Database initialized successfully.");
            break; // Success, exit retry loop
        }
        catch (Exception ex) when (attempt < maxRetries && IsConnectionException(ex))
        {
            logger.Warning(ex, "Database initialization failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay} seconds...", 
                attempt, maxRetries, retryDelay.TotalSeconds);
            
            Thread.Sleep(retryDelay);
            retryDelay = TimeSpan.FromSeconds(Math.Min(retryDelay.TotalSeconds * 1.5, 30)); // Exponential backoff
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Database initialization failed on attempt {Attempt}/{MaxRetries}.", attempt, maxRetries);
            
            if (attempt == maxRetries)
            {
                logger.Error("All {MaxRetries} database initialization attempts failed. Application will exit.", maxRetries);
                throw;
            }
        }
    }
}

static bool IsConnectionException(Exception ex)
{
    var exceptionMessage = ex.Message.ToLower();
    var innerExceptionMessage = ex.InnerException?.Message?.ToLower() ?? "";
    
    // Common connection-related error patterns
    var connectionErrors = new[]
    {
        "connection refused",
        "connection timeout", 
        "timeout expired",
        "network is unreachable",
        "host is unreachable",
        "connection reset",
        "connection failed",
        "unable to connect",
        "server is not ready",
        "database is starting up"
    };
    
    return connectionErrors.Any(error => 
        exceptionMessage.Contains(error) || innerExceptionMessage.Contains(error));
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("AllowedOrigins");

app.UsePublicViewerAuth();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    if (!app.Environment.IsDevelopment())
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; connect-src 'self'";

    await next();
});

app.UseAuthentication();

app.UseApiKeyAuthentication();

app.UseJwtTokenValidation();

app.UseAuthorization();

app.MapControllers();

app.Run();

static string MaskConnectionString(string connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
        return "[EMPTY]";

    var pairs = connectionString.Split(';');
    var maskedPairs = new List<string>();

    foreach (var pair in pairs)
    {
        var keyValue = pair.Split('=', 2);
        if (keyValue.Length == 2)
        {
            var key = keyValue[0].Trim().ToLowerInvariant();
            var value = keyValue[1].Trim();

            if (key.Contains("password") || key.Contains("pwd"))
                maskedPairs.Add($"{keyValue[0]}=***");
            else
                maskedPairs.Add(pair);
        }
        else
        {
            maskedPairs.Add(pair);
        }
    }

    return string.Join(";", maskedPairs);
}
