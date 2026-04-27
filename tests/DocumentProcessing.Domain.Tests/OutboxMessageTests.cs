using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Domain.Tests;

public class OutboxMessageTests
{
    private const string OutboxMessageType = "process-document-job";
    
    [Fact]
    public void Create_WithValidInputText_ShouldCreateOutboxMessage()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);

        Assert.NotNull(outboxMessage);
        Assert.Equal(OutboxMessageType, outboxMessage.Type);
        Assert.Equal(messageContent, outboxMessage.Content);
        Assert.Null(outboxMessage.PublishedOnUtc);
        Assert.Null(outboxMessage.ErrorMessage);
    }

    [Fact]
    public void Create_WithEmptyType_ShouldThrow()
    {
        var messageContent = Guid.NewGuid().ToString();
        Assert.Throws<ArgumentException>(() => OutboxMessage.Create("", messageContent));
    }
    
    [Fact]
    public void Create_WithEmptyContent_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => OutboxMessage.Create(OutboxMessageType, ""));
    }
    
    [Fact]
    public void MarkPublished_WhenAlreadyPublished_ShouldThrow()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);

        outboxMessage.MarkPublished();
        Assert.NotNull(outboxMessage.PublishedOnUtc);

        Assert.Throws<InvalidOperationException>(outboxMessage.MarkPublished);
    }

    [Fact]
    public void RecordError_WhenAlreadyPublished_ShouldThrow()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);

        outboxMessage.MarkPublished();
        
        Assert.Throws<InvalidOperationException>(() => outboxMessage.RecordError("Error"));
    }

    [Fact]
    public void RecordError_WithEmptyMessage_ShouldThrow()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);
        
        Assert.Throws<ArgumentException>(() => outboxMessage.RecordError(""));
    }

    [Fact]
    public void IncrementRetryCount_ShouldIncrementCount()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);
        
        outboxMessage.IncrementRetryCount();
        
        Assert.Equal(1, outboxMessage.RetryCount);
    }
    
    [Fact]
    public void RecordError_WhenCalledTwice_ShouldPreserveFirstError()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);
        outboxMessage.RecordError("First Error");
        
        outboxMessage.RecordError("Second Error");
        
        Assert.Equal("First Error", outboxMessage.ErrorMessage);
    }
    
    [Fact]
    public void IncrementRetryCount_WhenPublished_ShouldThrow()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);
        outboxMessage.MarkPublished();

        Assert.Throws<InvalidOperationException>(outboxMessage.IncrementRetryCount);
    }
    
    [Fact]
    public void AbandonMessage_ShouldSetAbandonMessageAtUtc()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);
        
        outboxMessage.AbandonMessage();
        
        Assert.NotNull(outboxMessage.AbandonedAtUtc);
    }
    
    [Fact]
    public void AbandonMessage_WhenPublished_ShouldThrow()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);
        outboxMessage.MarkPublished();

        Assert.Throws<InvalidOperationException>(outboxMessage.AbandonMessage);
        Assert.Null(outboxMessage.AbandonedAtUtc);
    }
        
    [Fact]
    public void AbandonMessage_WhenAbandoned_ShouldThrow()
    {
        var messageContent = Guid.NewGuid().ToString();
        var outboxMessage = OutboxMessage.Create(OutboxMessageType, messageContent);
        outboxMessage.AbandonMessage();

        Assert.Throws<InvalidOperationException>(outboxMessage.AbandonMessage);
    }
}