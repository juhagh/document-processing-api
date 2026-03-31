using System.Text;
using System.Text.Json;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DocumentProcessing.Infrastructure.Messaging;

public class RabbitMqJobMessagePublisher : IJobMessagePublisher
{

    private readonly ConnectionFactory _connectionFactory;
    private readonly RabbitMqOptions _options;

    public RabbitMqJobMessagePublisher(ConnectionFactory connectionFactory, IOptions<RabbitMqOptions> options)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
    }
    
    public async Task PublishAsync(ProcessDocumentJobMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: _options.QueueName,
            mandatory: false,
            body: body,
            cancellationToken: cancellationToken);
    }
}