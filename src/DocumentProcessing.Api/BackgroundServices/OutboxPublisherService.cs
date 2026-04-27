using System.Text.Json;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Messaging;
using DocumentProcessing.Application.Outbox;
using Microsoft.Extensions.Options;

namespace DocumentProcessing.Api.BackgroundServices;

public class OutboxPublisherService : BackgroundService
{
    private readonly OutboxOptions _options;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;


    public OutboxPublisherService(
        IOptions<OutboxOptions> outboxOptions,
        IServiceScopeFactory serviceScopeFactory, 
        ILogger<OutboxPublisherService> logger)
    {
        _options = outboxOptions.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox publisher started at {time}", DateTimeOffset.Now);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = _serviceScopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var publisher = scope.ServiceProvider.GetRequiredService<IJobMessagePublisher>();

            var messages = await repo.OutboxRepository.GetUnpublishedAsync(_options.BatchSize, stoppingToken);
            if (messages.Count > 0)
            {
                _logger.LogInformation("Processing {count} outbox messages", messages.Count);
            }
            
            foreach (var message in messages)
            {
                try
                {
                    switch (message.Type)
                    {
                        case OutboxMessageFactory.MessageType:
                        {
                            var jobMessage = JsonSerializer.Deserialize<ProcessDocumentJobMessage>(message.Content);
                            if (jobMessage == null)
                            {
                                message.RecordError("Serialized Document Job Message is null.");
                                _logger.LogError("Failed to deserialize message {id}", message.Id);
                                break;
                            }

                            if (message.RetryCount >= _options.MaxRetries)
                            {
                                var job = await repo.JobRepository.GetTrackedByIdAsync(jobMessage.JobId, stoppingToken);
                                if (job != null)
                                    job.MarkFailed("Max Retries exceeded.");
                                message.AbandonMessage();
                                message.RecordError("Max retries exceeded.");
                                
                                _logger.LogError("Max Retry Count exceeded for outbox message with ID: {id}", message.Id);
                                break;
                            }

                            await publisher.PublishAsync(jobMessage, stoppingToken);
                            message.MarkPublished();
                            _logger.LogDebug("Published message {id} of type {type}", message.Id, message.Type);
                            break;
                        }
                        default:
                            message.RecordError($"Unknown message type: {message.Type}");
                            _logger.LogWarning("Unknown message type {type} for message {id}", message.Type, message.Id);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    message.RecordError(ex.Message);
                    message.IncrementRetryCount();
                    _logger.LogError(ex, "Error processing outbox message {id}", message.Id);
                }
            }
            if (messages.Count > 0)
                await repo.CommitAsync(stoppingToken);
            
            await Task.Delay(_options.Interval, stoppingToken);   
        }
        
        _logger.LogInformation("Outbox publisher stopping at {time}", DateTimeOffset.Now);
    }

}