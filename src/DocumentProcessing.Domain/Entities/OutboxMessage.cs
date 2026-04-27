namespace DocumentProcessing.Domain.Entities;

public class OutboxMessage
{
    private OutboxMessage(Guid id, string type, string content)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        Id = id;
        Type = type;
        Content = content;
        CreatedAtUtc = DateTime.UtcNow;
    }
    
    private OutboxMessage() { }
    
    public Guid Id { get; private set; }
    public string Type { get; private set; } = null!;   
    public string Content { get; private set; } = null!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? PublishedOnUtc { get; private set; }
    public DateTime? AbandonedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }

    public static OutboxMessage Create(string type, string content)
    {
        return new OutboxMessage(Guid.NewGuid(), type, content);
    }

    public void MarkPublished()
    {
        if (PublishedOnUtc != null)
            throw new InvalidOperationException("Message already published");
        
        PublishedOnUtc = DateTime.UtcNow;    
        ErrorMessage = null;
    }

    public void RecordError(string error)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);
        if (PublishedOnUtc != null)
            throw new InvalidOperationException("Cannot record error after message is published.");
        
        if (ErrorMessage == null)
            ErrorMessage = error;
    }

    public void IncrementRetryCount()
    {
        if (PublishedOnUtc != null)
            throw new InvalidOperationException("Published message should not be retried");

        RetryCount += 1;
    }
    
    public void AbandonMessage()
    {
        if (PublishedOnUtc != null)
            throw new InvalidOperationException("Can not abandon already published message");

        if (AbandonedAtUtc != null)
            throw new InvalidOperationException("Can not abandon already abandoned message");

        AbandonedAtUtc = DateTime.UtcNow;    
    }
}