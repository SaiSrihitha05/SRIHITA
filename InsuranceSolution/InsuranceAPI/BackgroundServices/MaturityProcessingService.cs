using Application.Services;

namespace InsuranceAPI.BackgroundServices
{
    public class MaturityProcessingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MaturityProcessingService> _logger;

        public MaturityProcessingService(
            IServiceScopeFactory scopeFactory,
            ILogger<MaturityProcessingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var claimService = scope.ServiceProvider
                        .GetRequiredService<IClaimService>();

                    await claimService.ProcessMaturityClaimsAsync();

                    _logger.LogInformation(
                        "Maturity processing completed at {Time}",
                        DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing maturity claims");
                }

                // Run once daily
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}