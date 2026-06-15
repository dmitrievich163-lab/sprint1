using AspNetCoreApi.Models;

namespace AspNetCoreApi.Services
{
    public class BookingProcessingHostedService : BackgroundService
    {
        private readonly InMemoryBookingRepository _bookingRepository;
        private readonly IEventService _eventRepository;
        private readonly ILogger<BookingProcessingHostedService> _logger;

        private readonly SemaphoreSlim _processingSemaphore = new(1, 1);

        public BookingProcessingHostedService(
            InMemoryBookingRepository bookingRepository,
            IEventService eventRepository, 
            ILogger<BookingProcessingHostedService> logger)
        {
            _bookingRepository = bookingRepository;
            _eventRepository = eventRepository; 
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновая обработка бронирований запущена.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var pendingBookings = _bookingRepository.GetAll()
                        .Where(b => b.Status == BookingStatus.Pending)
                        .ToList();

                    if (pendingBookings.Any())
                    {
                        _logger.LogInformation($"Обнаружено {pendingBookings.Count} бронирований для обработки.");

                        var processingTasks = pendingBookings.Select(booking =>
                            ProcessBookingAsync(booking, stoppingToken)
                        );

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

        private async Task ProcessBookingAsync(Booking booking, CancellationToken stoppingToken)
        {
            _logger.LogDebug($"Начало параллельной обработки брони {booking.Id}");

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                await _processingSemaphore.WaitAsync(stoppingToken);

                stoppingToken.ThrowIfCancellationRequested();

                if (booking.Status != BookingStatus.Pending)
                {
                    _logger.LogWarning($"Пропуск обработки брони {booking.Id}, так как её статус уже изменился на {booking.Status}.");
                    return;
                }

                var @event = _eventRepository.GetById(booking.EventId);
                if (@event == null)
                {
                    _logger.LogWarning($"Событие для брони {booking.Id} не найдено. Отклонение брони.");
                    booking.Reject();
                    _bookingRepository.Add(booking);
                    return;
                }

                booking.Confirm();
                _bookingRepository.Add(booking);

                _logger.LogInformation($"Бронь {booking.Id} успешно обработана. Новый статус: {booking.Status}");
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug($"Обработка брони {booking.Id} была отменена.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ошибка при обработке брони {booking.Id}. Отмена операции.");

                try
                {
                    if (booking.Status == BookingStatus.Pending)
                    {
                        booking.Reject();
                        _bookingRepository.Add(booking);

                        var @event = _eventRepository.GetById(booking.EventId);
                        @event?.ReleaseSeats(1); 
                        _eventRepository?.Update(@event.Id,@event); 
                    }
                }
                catch (Exception innerEx)
                {
                    _logger.LogError(innerEx, $"Критическая ошибка при попытке откатить изменения для брони {booking.Id}.");
                }
            }
            finally
            {
                if (_processingSemaphore.CurrentCount == 0)
                {
                    _processingSemaphore.Release();
                }
            }
        }
    }
}
