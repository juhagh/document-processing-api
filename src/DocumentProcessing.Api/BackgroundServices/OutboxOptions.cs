namespace DocumentProcessing.Api.BackgroundServices;

public class OutboxOptions
{
    // The interval at which the background service polls the database
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(5);
    
    // Number of messages to process in a single batch
    public int BatchSize { get; set; } = 5;

    public int MaxRetries { get; set; } = 3;
}