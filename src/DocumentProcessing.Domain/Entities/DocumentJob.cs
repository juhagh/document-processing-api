using DocumentProcessing.Domain.Enums;
using DocumentProcessing.Domain.ValueObjects;

namespace DocumentProcessing.Domain.Entities;

public class DocumentJob
{
    private DocumentJob(string inputText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputText);
        
        var timeStamp = DateTime.UtcNow;

        Id = Guid.NewGuid();
        InputText = inputText;
        Status = JobStatus.Pending;
        SubmittedAtUtc = timeStamp;
        UpdatedAtUtc = timeStamp;
    }
    
    private DocumentJob() { }
    
    // Job
    public Guid Id { get; private set; }
    public string InputText { get; private set; } = null!;
    public JobStatus Status { get; private set; }
    public DateTime SubmittedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    
    // Result
    public int? WordCount { get; private set; }
    public int? CharacterCount { get; private set; }
    public int? LineCount { get; private set; }
    public int? KeywordHits { get; private set; }
    public string? Category { get; private set; }
    public string? Summary { get; private set; }

    public static DocumentJob Create(string inputText)
    {
        return new DocumentJob(inputText);
    }
    
    public void MarkQueued()
    {
        if (Status != JobStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot mark job as queued when status is {Status}.");

        Status = JobStatus.Queued;
        
        Touch();
    }

    public void MarkProcessing()
    {
        if (Status != JobStatus.Queued)
            throw new InvalidOperationException(
                $"Cannot mark job as processing when status is {Status}.");

        Status = JobStatus.Processing;
        
        Touch();
    }

    public void MarkCompleted(DocumentAnalysisResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        
        if (Status != JobStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot mark job as completed when status is {Status}.");

        WordCount = result.WordCount;
        CharacterCount = result.CharacterCount;
        LineCount = result.LineCount;
        KeywordHits = result.KeywordHits;
        Category = result.Category;
        Summary = result.Summary;
        
        ErrorMessage = null;
        Status = JobStatus.Completed;
        
        var timeNow = DateTime.UtcNow;
        
        UpdatedAtUtc = timeNow;
        CompletedAtUtc = timeNow;
    }

    public void MarkFailed(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);
        
        if (Status != JobStatus.Processing)
            throw new InvalidOperationException(
                $"Cannot mark job as failed when status is {Status}.");

        Status = JobStatus.Failed;
        ErrorMessage = errorMessage;
        Touch();
    }
    
    public void AbandonJob(string errorMessage)
    {
        if (Status != JobStatus.Queued)
            throw new InvalidOperationException($"Cannot mark job dispatch failed when status is {Status}.");

        Status = JobStatus.Failed;
        ErrorMessage = errorMessage;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}