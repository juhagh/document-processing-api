using System.Text.Json;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Messaging;
using DocumentProcessing.Domain.Enums;
using DocumentProcessing.Infrastructure.Messaging;
using DocumentProcessing.Worker.Services;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DocumentProcessing.Worker.Consumers;

public class DocumentJobConsumer : BackgroundService
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _options;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<DocumentJobConsumer> _logger;
    private readonly IDocumentAnalysisService _analysisService;

    public DocumentJobConsumer(
        ConnectionFactory connectionFactory, 
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory serviceScopeFactory, 
        ILogger<DocumentJobConsumer> logger,
        IDocumentAnalysisService documentAnalysisService)
    {
        _connectionFactory = connectionFactory;
        _options = rabbitMqOptions.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _analysisService = documentAnalysisService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            await HandleMessageAsync(channel, ea, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleMessageAsync(
        IChannel channel,
        BasicDeliverEventArgs ea,
        CancellationToken cancellationToken)
    {
        var parsed = TryGetDeathCount(
            ea.BasicProperties,
            out var retryCount,
            out var malformedXDeathHeader);
        
        var body = ea.Body.ToArray();
        var message = JsonSerializer.Deserialize<ProcessDocumentJobMessage>(body);
        
        if (message is null)
        {
            _logger.LogWarning("Received invalid or empty ProcessDocumentJobMessage.");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }
        
        if (!parsed && malformedXDeathHeader)
        {
            // Treat as poison message.
            // Do not requeue indefinitely.
            _logger.LogWarning("Malformed x-death header detected. Nack message without requeue. JobId: {JobId}.",
                message.JobId);
            
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }
        
        _logger.LogInformation("Received job message for job {JobId}.", message.JobId);
        
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        
        var repo = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        try
        {
            var job = await repo.JobRepository.GetTrackedByIdAsync(message.JobId, cancellationToken);
            if (job is null)
            {
                _logger.LogWarning("Job {JobId} was not found.", message.JobId);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
                return;
            }
            
            if (retryCount >= _options.MaxRetries)
            {
                // Max retry attempts reached.
                _logger.LogWarning("MaxRetries exceeded. Nack message without requeue. JobId: {JobId}.", message.JobId);
                // Queued -> Processing -> Failed required since no intermediate commit exists.
                // See known limitation: retry queue pattern not yet implemented.
                job.MarkProcessing();
                job.MarkFailed("MaxRetries exceeded.");
                await repo.CommitAsync(cancellationToken);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
                return;
            }
            
            if (job.Status != JobStatus.Queued) 
            {
                _logger.LogWarning(
                    "Job {JobId} was in status {Status}, expected Queued. Nack message without requeue.",
                    job.Id,
                    job.Status);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, cancellationToken);
                return;
            }
            
            job.MarkProcessing();
            // Commit once RabbitMQ retry exchange/queue are implemented

            _logger.LogInformation("Started processing job {JobId}.", job.Id);
            
            var result = _analysisService.Analyze(job.InputText);
            
            job.MarkCompleted(result);

            await repo.CommitAsync(cancellationToken);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Completed processing job {JobId}.", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process job {JobId}.", message.JobId);
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }
    
    private static bool TryGetDeathCount(
        IReadOnlyBasicProperties properties,
        out long deathCount,
        out bool malformedXDeathHeader)
    {
        deathCount = 0;
        malformedXDeathHeader = false;

        if (properties.Headers is not { } headers ||
            !headers.TryGetValue("x-death", out var rawXDeath))
        {
            return true; // no retry history yet
        }

        if (rawXDeath is not IList<object> xDeathList ||
            xDeathList.Count == 0 ||
            xDeathList[0] is not IDictionary<string, object> firstEntry ||
            !firstEntry.TryGetValue("count", out var rawCount))
        {
            // Something wrong with headers
            malformedXDeathHeader = true;
            return false; 
        }

        deathCount = rawCount switch
        {
            long value => value,
            int value => value,
            short value => value,
            byte value => value,
            _ => -1
        };

        if (deathCount < 0)
        {
            // Something wrong with headers
            malformedXDeathHeader = true;
            deathCount = 0;
            return false;
        }

        return true;
    }
}