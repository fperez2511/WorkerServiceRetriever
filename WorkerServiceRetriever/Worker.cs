namespace WorkerServiceRetriever
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IVaultClient _vaultClient;

        public Worker(ILogger<Worker> logger, IVaultClient vaultClient)
        {
            _logger = logger;
            _vaultClient = vaultClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _vaultClient.ExecuteAsync(stoppingToken);
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}