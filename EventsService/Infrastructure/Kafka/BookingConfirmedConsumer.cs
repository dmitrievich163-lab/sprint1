using System.Text.Json;
using Confluent.Kafka;
using EventsService.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedContracts;

namespace EventsService.Infrastructure.Kafka;

public class BookingConfirmedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingConfirmedConsumer> _logger;
    private readonly ConsumerConfig _config;
    private readonly string _topic;

    public BookingConfirmedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingConfirmedConsumer> logger,
        ConsumerConfig config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;
        _topic = KafkaTopics.BookingConfirmed;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingConfirmedConsumer starting, subscribing to topic: {Topic}", _topic);

        using var consumer = new ConsumerBuilder<string, string>(_config).Build();
        consumer.Subscribe(_topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(stoppingToken);

                    _logger.LogInformation(
                        "Received message from partition {Partition}, offset {Offset}: {Key}",
                        consumeResult.Partition.Value,
                        consumeResult.Offset.Value,
                        consumeResult.Message.Key);

                    var evt = JsonSerializer.Deserialize<BookingConfirmed>(consumeResult.Message.Value);
                    if (evt == null)
                    {
                        _logger.LogWarning("Failed to deserialize BookingConfirmed message, skipping.");
                        continue;
                    }

                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<EventsDbContext>();

                    var @event = await context.Events.FirstOrDefaultAsync(e => e.Id == evt.EventId, stoppingToken);
                    if (@event == null)
                    {
                        _logger.LogWarning("Event {EventId} not found, skipping seat decrement.", evt.EventId);
                        continue;
                    }

                    if (@event.AvailableSeats < evt.SeatCount)
                    {
                        _logger.LogWarning(
                            "Not enough seats for event {EventId}. Available: {Available}, requested: {Requested}. Skipping.",
                            @event.Id, @event.AvailableSeats, evt.SeatCount);
                        continue;
                    }

                    @event.TryReserveSeats(evt.SeatCount);
                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation(
                        "Decremented {Count} seats for event {EventId}. New available: {Available}.",
                        evt.SeatCount, @event.Id, @event.AvailableSeats);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka consume error.");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in BookingConfirmedConsumer loop.");
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("BookingConfirmedConsumer stopped.");
        }
    }
}
