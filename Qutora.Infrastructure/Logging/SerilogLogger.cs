using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Qutora.Infrastructure.Logging;

public static class SerilogLogger
{
    public static Serilog.ILogger ConfigureLogger(string applicationName, string environment)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.WithProperty("ApplicationName", applicationName)
            .Enrich.WithProperty("Environment", environment)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                "logs/qutora-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
            )
            .CreateLogger();
    }

    public static ILogger<T> CreateLogger<T>()
    {
        var factory = LoggerFactory.Create(builder => builder.AddSerilog());
        return factory.CreateLogger<T>();
    }
}
