using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public class BookingProcessingHostedService: BackgroundService
    {
        private readonly InMemoryBookingRepository _repository;
        private readonly ILogger<BookingProcessingHostedService> _logger;

        public BookingProcessingHostedService(
            InMemoryBookingRepository repository,
            ILogger<BookingProcessingHostedService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновая обработка бронирований запущена.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pendingBookings = _repository.GetAll()
                        .Where(b => b.Status == BookingStatus.Pending)
                        .ToList();

                    foreach (var booking in pendingBookings)
                    {
                        _logger.LogInformation($"Начало обработки брони {booking.Id}");

                        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                        booking.Confirm(); 

                        _logger.LogInformation($"Бронь {booking.Id} обработана. Новый статус: {booking.Status}");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Произошла ошибка при обработке бронирований.");
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            _logger.LogInformation("Фоновая обработка бронирований остановлена.");
        }
    }
}
