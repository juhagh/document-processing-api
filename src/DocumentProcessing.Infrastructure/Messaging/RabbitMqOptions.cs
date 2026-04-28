namespace DocumentProcessing.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public string HostName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = string.Empty;
    public int Port { get; set; } = 5672;
    public string ClientName { get; set; } = string.Empty;
    public bool AutomaticRecoveryEnabled { get; set; } = true;
    public int RequestedHeartbeat { get; set; } = 30;
    public string QueueName { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
}