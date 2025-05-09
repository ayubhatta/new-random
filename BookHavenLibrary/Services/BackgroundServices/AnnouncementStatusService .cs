using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using BookHavenLibrary.Models;
using BookHavenLibrary.Data;

namespace BookHavenLibrary.Services.BackgroundServices
{
    public class AnnouncementStatusService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AnnouncementStatusService> _logger;    

        public AnnouncementStatusService(IServiceProvider serviceProvider, ILogger<AnnouncementStatusService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var expiredAnnouncements = await dbContext.Announcements
                            .Where(a => a.IsActive && a.EndDate <= DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        if (expiredAnnouncements.Any())
                        {
                            foreach (var ann in expiredAnnouncements)
                            {
                                ann.IsActive = false;
                            }

                            await dbContext.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation($"{expiredAnnouncements.Count} announcements deactivated at {DateTime.UtcNow}.");
                        }
                    }

                    // Wait 1 minutes before checking again
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking announcement statuses.");
                }
            }
        }
    }
}
