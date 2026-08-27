using BookingsService.Application.Repositories;
using BookingsService.Application.Services;
using BookingsService.Infrastructure.DataAccess;
using BookingsService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingsService.Presentation;

public class BookingProcessingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessingHostedService> _logger;

    public BookingProcessingHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingProcessingHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновая обработка бронирований запущена.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<BookingsDbContext>();

                var pendingBookingIds = await context.Bookings
                    .AsNoTracking()
                    .Where(b => b.Status == BookingStatus.Pending)
                    .Select(b => b.Id)
                    .ToListAsync(stoppingToken);

                if (pendingBookingIds.Any())
                {
                    _logger.LogInformation($"Обнаружено {pendingBookingIds.Count} бронирований для обработки.");

                    var processingTasks = pendingBookingIds.Select(async bookingId =>
                    {
                        using var processingScope = _scopeFactory.CreateScope();
                        var bookingService = processingScope.ServiceProvider.GetRequiredService<IBookingService>();
                        var scopedLogger = processingScope.ServiceProvider.GetRequiredService<ILogger<BookingProcessingHostedService>>();

                        try
                        {
                            scopedLogger.LogDebug($"Начало обработки брони {bookingId}");
                            await bookingService.ProcessPendingBookingAsync(bookingId);
                            scopedLogger.LogInformation($"Бронь {bookingId} успешно обработана.");
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            scopedLogger.LogError(ex, $"Ошибка при обработке брони {bookingId}.");
                        }
                    });

                    await Task.WhenAll(processingTasks);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Произошла критическая ошибка в цикле обработки бронирований.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("Фоновая обработка бронирований остановлена.");
    }
}
