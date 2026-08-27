using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedContracts;

namespace EventsService.Infrastructure.Kafka;

public class KafkaTopicInitializer : IHostedService
{
    private readonly ILogger<KafkaTopicInitializer> _logger;
    private readonly ProducerConfig _config;

    public KafkaTopicInitializer(ILogger<KafkaTopicInitializer> logger, ProducerConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var adminClient = new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = _config.BootstrapServers }).Build();

            var topicSpec = new TopicSpecification
            {
                Name = KafkaTopics.BookingConfirmed,
                NumPartitions = 1,
                ReplicationFactor = 1
            };

            var result = adminClient.CreateTopicsAsync(
                new[] { topicSpec });

            _logger.LogInformation("Kafka topic '{Topic}' created successfully.", KafkaTopics.BookingConfirmed);
        }
        catch (CreateTopicsException ex) when (ex.Error.Reason.Contains("already exists"))
        {
            _logger.LogInformation("Kafka topic '{Topic}' already exists, skipping creation.", KafkaTopics.BookingConfirmed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create Kafka topic '{Topic}'. It may need to be created manually.", KafkaTopics.BookingConfirmed);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
