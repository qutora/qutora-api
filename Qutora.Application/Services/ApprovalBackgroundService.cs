using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qutora.Application.Interfaces;

namespace Qutora.Application.Services;

public class ApprovalBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<ApprovalBackgroundService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);
    
    // Circuit breaker pattern to prevent infinite loops
    private int _consecutiveFailures = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private readonly int _maxConsecutiveFailures = 5;
    private readonly TimeSpan _circuitBreakerTimeout = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Approval Background Service started with circuit breaker protection");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Circuit breaker check
                if (IsCircuitBreakerOpen())
                {
                    logger.LogWarning("Circuit breaker is OPEN. Waiting {Timeout} minutes before retry", 
                        _circuitBreakerTimeout.TotalMinutes);
                    await Task.Delay(_circuitBreakerTimeout, stoppingToken);
                    continue;
                }

                await ProcessExpiredApprovalRequestsAsync(stoppingToken);
                
                // Reset failure count on success
                _consecutiveFailures = 0;
                
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Approval Background Service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                _lastFailureTime = DateTime.UtcNow;
                
                logger.LogError(ex, "Error occurred in Approval Background Service. Consecutive failures: {Count}/{Max}", 
                    _consecutiveFailures, _maxConsecutiveFailures);

                // Exponential backoff with circuit breaker
                var delay = CalculateBackoffDelay();
                await Task.Delay(delay, stoppingToken);
            }
        }

        logger.LogInformation("Approval Background Service stopped");
    }

    /// <summary>
    /// Checks if circuit breaker should be open (stop processing)
    /// </summary>
    private bool IsCircuitBreakerOpen()
    {
        if (_consecutiveFailures < _maxConsecutiveFailures)
            return false;

        var timeSinceLastFailure = DateTime.UtcNow - _lastFailureTime;
        return timeSinceLastFailure < _circuitBreakerTimeout;
    }

    /// <summary>
    /// Calculates exponential backoff delay
    /// </summary>
    private TimeSpan CalculateBackoffDelay()
    {
        if (_consecutiveFailures >= _maxConsecutiveFailures)
        {
            // Circuit breaker activated - use longer delay
            return _circuitBreakerTimeout;
        }

        // Exponential backoff: 1min, 2min, 4min, 8min, 16min
        var backoffMinutes = Math.Min(Math.Pow(2, _consecutiveFailures - 1), 16);
        return TimeSpan.FromMinutes(backoffMinutes);
    }

    private async Task ProcessExpiredApprovalRequestsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var approvalService = scope.ServiceProvider.GetRequiredService<IApprovalService>();

        try
        {
            await approvalService.ProcessExpiredRequestsAsync(cancellationToken);
            logger.LogDebug("Expired approval requests processed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process expired approval requests");
        }
    }
}
