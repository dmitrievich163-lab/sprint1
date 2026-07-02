using AspNetCoreApi.DataAccess;
using AspNetCoreApi.Models;
using AspNetCoreApi.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreApi.Services
{
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
                    // Создаем scope для получения доступа к scoped-сервисам (DbContext, BookingService)
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

                    // 1. Находим ID всех бронирований со статусом Pending
                    // Используем Select для оптимизации - загружаем только ID, а не все объекты целиком.
                    var pendingBookingIds = await context.Bookings
                        .AsNoTracking() // Не отслеживаем изменения, так как только читаем ID
                        .Where(b => b.Status == BookingStatus.Pending)
                        .Select(b => b.Id)
                        .ToListAsync(stoppingToken);

                    if (pendingBookingIds.Any())
                    {
                        _logger.LogInformation($"Обнаружено {pendingBookingIds.Count} бронирований для обработки.");

                        // 2. Для каждой брони создаем отдельную задачу (и отдельный scope внутри)
                        var processingTasks = pendingBookingIds.Select(async bookingId =>
                        {
                            // ВАЖНО: Создаем НОВЫЙ scope для каждой операции,
                            // чтобы гарантировать атомарность и свой DbContext.
                            using var processingScope = _scopeFactory.CreateScope();
                            var scopedBookingService = processingScope.ServiceProvider.GetRequiredService<IBookingService>();
                            var scopedLogger = processingScope.ServiceProvider.GetRequiredService<ILogger<BookingProcessingHostedService>>();

                            try
                            {
                                scopedLogger.LogDebug($"Начало обработки брони {bookingId}");
                                await scopedBookingService.ProcessPendingBookingAsync(bookingId);
                                scopedLogger.LogInformation($"Бронь {bookingId} успешно обработана.");
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                scopedLogger.LogError(ex, $"Ошибка при обработке брони {bookingId}.");
                                // Логика отката должна быть внутри BookingService.ProcessPendingBookingAsync
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
}