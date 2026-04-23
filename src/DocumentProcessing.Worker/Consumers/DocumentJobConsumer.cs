using System.Text.Json;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Messaging;
using DocumentProcessing.Domain.Entities;
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

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: stoppingToken);

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
        var body = ea.Body.ToArray();
        var message = JsonSerializer.Deserialize<ProcessDocumentJobMessage>(body);
        
        if (message is null)
        {
            _logger.LogWarning("Received invalid or empty ProcessDocumentJobMessage.");
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            return;
        }
        
        _logger.LogInformation("Received job message for job {JobId}.", message.JobId);
        
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        
        var repo = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        DocumentJob? job = null;
        
        try
        {
            job = await repo.JobRepository.GetTrackedByIdAsync(message.JobId, cancellationToken);
            if (job is null)
            {
                _logger.LogWarning("Job {JobId} was not found.", message.JobId);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }

            if (job.Status != JobStatus.Queued)
            {
                _logger.LogWarning(
                    "Job {JobId} was in status {Status}, expected Queued. Message will be acknowledged.",
                    job.Id,
                    job.Status);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }
            
            job.MarkProcessing();
            await repo.CommitAsync(cancellationToken);

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

            if (job is not null && job.Status == JobStatus.Processing)
            {
                job.MarkFailed(ex.Message);
                await repo.CommitAsync(cancellationToken);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
    }
}