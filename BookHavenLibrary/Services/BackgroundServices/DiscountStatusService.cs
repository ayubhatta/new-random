using BookHavenLibrary.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookHavenLibrary.Services.BackgroundServices
{
    public class DiscountStatusService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DiscountStatusService> _logger;

        public DiscountStatusService(IServiceProvider serviceProvider, ILogger<DiscountStatusService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var discountRepo = scope.ServiceProvider.GetRequiredService<IDiscountRepository>();

                var expired = await discountRepo.GetExpiredDiscountsAsync();
                foreach (var d in expired)
                {
                    d.IsActive = false;
                    d.OnSale = false;
                    await discountRepo.UpdateAsync(d);
                }

                if (expired.Any())
                    _logger.LogInformation($"{expired.Count()} discounts expired at {DateTime.UtcNow}");

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
