using ViabilityIQ.Application.Interfaces;

namespace ViabilityIQ.Web.Services;

public sealed class SupportConversationExpiryService(
    IServiceScopeFactory scopeFactory,
    ILogger<SupportConversationExpiryService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var supportService = scope.ServiceProvider.GetRequiredService<ISupportService>();
                await supportService.ExpireIdleConversationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unable to expire idle support conversations.");
            }
        }
    }
}
