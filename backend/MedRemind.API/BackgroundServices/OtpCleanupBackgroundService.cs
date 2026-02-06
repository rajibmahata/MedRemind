using MedRemind.Services.Communication;
using Microsoft.Extensions.DependencyInjection;

namespace MedRemind.API.BackgroundServices;

/// <summary>
/// Background service that automatically cleans up expired OTP codes every 5 minutes
/// </summary>
public class OtpCleanupBackgroundService : IHostedService, IDisposable
{
    private readonly ILogger<OtpCleanupBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private Timer? _timer;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

    public OtpCleanupBackgroundService(
        ILogger<OtpCleanupBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? OTP Cleanup Background Service started");

        _timer = new Timer(DoWork, null, TimeSpan.Zero, _cleanupInterval);

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            _logger.LogInformation("? Running OTP cleanup...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var otpService = scope.ServiceProvider.GetRequiredService<OtpCodeService>();
                var deletedCount = await otpService.CleanupExpiredOtpsAsync();
                
                if (deletedCount > 0)
                {
                    _logger.LogInformation("? Cleaned up {Count} expired OTPs", deletedCount);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error during OTP cleanup");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? OTP Cleanup Background Service stopped");

        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
