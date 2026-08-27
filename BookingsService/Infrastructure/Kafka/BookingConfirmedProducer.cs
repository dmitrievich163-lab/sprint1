using System.Text.Json;
using Confluent.Kafka;
using BookingsService.Application.Services;
using SharedContracts;
using Microsoft.Extensions.Logging;

namespace BookingsService.Infrastructure.Kafka;

public class BookingConfirmedProducer : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<BookingConfirmedProducer> _logger;

    public BookingConfirmedProducer(ProducerConfig config, ILogger<BookingConfirmedProducer> logger)
    {
        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }

    public async Task PublishBookingConfirmedAsync(BookingConfirmed evt)
    {
        var message = new Message<string, string>
        {
            Key = evt.EventId.ToString(),
            Value = JsonSerializer.Serialize(evt)
        };

        var result = await _producer.ProduceAsync(KafkaTopics.BookingConfirmed, message);

        _logger.LogInformation(
            "Published BookingConfirmed for event {EventId}, booking {BookingId} to partition {Partition} at offset {Offset}",
            evt.EventId, evt.BookingId, result.Partition.Value, result.Offset.Value);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
