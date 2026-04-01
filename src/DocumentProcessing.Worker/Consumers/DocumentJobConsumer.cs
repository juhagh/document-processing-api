using System.Text.Json;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Messaging;
using DocumentProcessing.Domain.Entities;
using DocumentProcessing.Domain.Enums;
using DocumentProcessing.Domain.ValueObjects;
using DocumentProcessing.Infrastructure.Messaging;
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

    public DocumentJobConsumer(
        ConnectionFactory connectionFactory, 
        IOptions<RabbitMqOptions> rabbitMqOptions,
        IServiceScopeFactory serviceScopeFactory, 
        ILogger<DocumentJobConsumer> logger)
    {
        _connectionFactory = connectionFactory;
        _options = rabbitMqOptions.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
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
        // deserialize
        var body = ea.Body.ToArray();
        var message = JsonSerializer.Deserialize<ProcessDocumentJobMessage>(body);
        
        if (message is null)
        {
            _logger.LogWarning("Received invalid or empty ProcessDocumentJobMessage.");
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            return;
        }
        
        _logger.LogInformation("Received job message for job {JobId}.", message.JobId);

        // create scope
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        
        // resolve repository
        var repo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        
        DocumentJob? job = null;
        
        try
        {
            job = await repo.GetTrackedByIdAsync(message.JobId, cancellationToken);
            if (job is null)
            {
                _logger.LogWarning("Job {JobId} was not found.", message.JobId);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }
            
            _logger.LogInformation("Started processing job {JobId}.", job.Id);

            if (job.Status != JobStatus.Queued)
            {
                _logger.LogWarning(
                    "Job {JobId} was in status {Status}, expected Queued. Message will be acknowledged.",
                    job.Id,
                    job.Status);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
                return;
            }
            
            // mark processing
            job.MarkProcessing();
            await repo.SaveChangesAsync(cancellationToken);

            // analyze
            var result = Analyze(job.InputText);
            
            // mark completed/failed
            // if success
            job.MarkCompleted(result);
            
            // Tell RabbitMQ the message was handled successfully
            
            await repo.SaveChangesAsync(cancellationToken);
            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            
            _logger.LogInformation("Completed processing job {JobId}.", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process job {JobId}.", message.JobId);

            if (job is not null && job.Status == JobStatus.Processing)
            {
                job.MarkFailed(ex.Message);
                await repo.SaveChangesAsync(cancellationToken);
            }

            await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
        }
    }

    private static DocumentAnalysisResult Analyze(string inputText)
    {
        var characterCount = inputText.Length;
        var lineCount = inputText.Split('\n').Length;
        var wordCount = 0;
        var inWord = false;

        foreach (var c in inputText.AsSpan())
        {
            if (char.IsWhiteSpace(c))
                inWord = false;
            else if (!inWord)
            {
                inWord = true;
                wordCount++;
            }
        }

        var keywordHits = 0;
        var category = "General";
        var summary = inputText.Length <= 120
            ? inputText
            : inputText[..120] + "...";

        return new DocumentAnalysisResult(
            WordCount: wordCount,
            CharacterCount: characterCount,
            LineCount: lineCount,
            KeywordHits: keywordHits,
            Category: category,
            Summary: summary);
    }
}