namespace ST1Savall.API.Services;

public sealed class PeriodicidadObraHostedService(IServiceScopeFactory scopeFactory, ILogger<PeriodicidadObraHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                await scope.ServiceProvider.GetRequiredService<PeriodicidadObraService>().SincronizarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Error al sincronizar periodicidades de obra."); }
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}