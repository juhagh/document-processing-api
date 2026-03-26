using DocumentProcessing.Domain.Entities;
using DocumentProcessing.Domain.Enums;
using DocumentProcessing.Domain.ValueObjects;

namespace DocumentProcessing.Domain.Tests;

public class DocumentJobTests
{
    [Fact]
    public void Create_WithValidInputText_ShouldInitializePendingJob()
    {
        var job = CreateJobInState(JobStatus.Pending);
        
        Assert.Equal(JobStatus.Pending, job.Status);
        Assert.NotEqual(default, job.UpdatedAtUtc);
        Assert.Equal(job.SubmittedAtUtc, job.UpdatedAtUtc);
        Assert.Equal("Test", job.InputText);
    }
    
    [Fact]
    public void Create_WhenInputTextIsEmpty_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => DocumentJob.Create(""));
    }
    
    [Fact]
    public void MarkQueued_WhenPending_ShouldSetStatusToQueued()
    {
        var job = CreateJobInState(JobStatus.Pending);
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        
        job.MarkQueued();
        
        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.True(job.UpdatedAtUtc >= originalUpdatedAtUtc);
    }

    [Theory]
    [InlineData(JobStatus.Queued)]
    [InlineData(JobStatus.Processing)]
    [InlineData(JobStatus.Completed)]
    [InlineData(JobStatus.Failed)]
    public void MarkQueued_WhenStatusIsNotPending_ShouldThrow(JobStatus initialStatus)
    {
        var job = CreateJobInState(initialStatus);
        var originalStatus = job.Status;
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        
        Assert.Throws<InvalidOperationException>(() => job.MarkQueued());
        Assert.Equal(originalStatus, job.Status);
        Assert.Equal(originalUpdatedAtUtc, job.UpdatedAtUtc);
    }

    [Fact]
    public void MarkProcessing_WhenQueued_ShouldSetStatusToProcessing()
    {
        var job = CreateJobInState(JobStatus.Queued);
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        
        job.MarkProcessing();
        
        Assert.Equal(JobStatus.Processing, job.Status);
        Assert.True(job.UpdatedAtUtc >= originalUpdatedAtUtc);
    }

    [Theory]
    [InlineData(JobStatus.Pending)]
    [InlineData(JobStatus.Processing)]
    [InlineData(JobStatus.Completed)]
    [InlineData(JobStatus.Failed)]
    public void MarkProcessing_WhenStatusIsNotQueued_ShouldThrow(JobStatus initialStatus)
    {
        var job = CreateJobInState(initialStatus);
        var originalStatus = job.Status;
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        
        Assert.Throws<InvalidOperationException>(() => job.MarkProcessing());
        Assert.Equal(originalStatus, job.Status);
        Assert.Equal(originalUpdatedAtUtc, job.UpdatedAtUtc);
    }
    
    [Fact]
    public void MarkCompleted_WhenProcessing_ShouldSetStatusToCompleted()
    {
        var job = CreateJobInState(JobStatus.Processing);
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        
        job.MarkCompleted(ValidResult);
        
        Assert.Equal(JobStatus.Completed, job.Status);
        AssertResultMatches(job, ValidResult);
        Assert.True(job.UpdatedAtUtc >= originalUpdatedAtUtc);
        Assert.NotNull(job.CompletedAtUtc);
        Assert.Equal(job.UpdatedAtUtc, job.CompletedAtUtc);
    }

    [Theory]
    [InlineData(JobStatus.Pending)]
    [InlineData(JobStatus.Queued)]
    [InlineData(JobStatus.Completed)]
    [InlineData(JobStatus.Failed)]
    public void MarkCompleted_WhenStatusIsNotProcessing_ShouldThrow(JobStatus initialStatus)
    {
        var job = CreateJobInState(initialStatus);
        var originalStatus = job.Status;
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        var originalCompletedAtUtc = job.CompletedAtUtc;
        var originalErrorMessage = job.ErrorMessage;
        
        
        Assert.Throws<InvalidOperationException>(() => job.MarkCompleted(ValidResult));
        Assert.Equal(originalStatus, job.Status);
        Assert.Equal(originalUpdatedAtUtc, job.UpdatedAtUtc);
        Assert.Equal(originalCompletedAtUtc, job.CompletedAtUtc);
        Assert.Equal(originalErrorMessage, job.ErrorMessage);
    }

    [Fact]
    public void MarkCompleted_WhenResultIsNull_ShouldThrow()
    {
        var job = CreateJobInState(JobStatus.Processing);
        var originalStatus = job.Status;
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        
        Assert.Throws<ArgumentNullException>(() => job.MarkCompleted(null));
        Assert.Equal(originalStatus, job.Status);
        Assert.Equal(originalUpdatedAtUtc, job.UpdatedAtUtc);
    }
    
    [Fact]
    public void MarkFailed_WhenProcessing_ShouldSetStatusToFailed()
    {
        var job = CreateJobInState(JobStatus.Processing);
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        
        job.MarkFailed("Failed");
        
        Assert.Equal(JobStatus.Failed, job.Status);
        Assert.Equal("Failed", job.ErrorMessage);
        Assert.True(job.UpdatedAtUtc >= originalUpdatedAtUtc);
    }

    [Theory]
    [InlineData(JobStatus.Pending)]
    [InlineData(JobStatus.Queued)]
    [InlineData(JobStatus.Completed)]
    [InlineData(JobStatus.Failed)]
    public void MarkFailed_WhenStatusIsNotProcessing_ShouldThrow(JobStatus initialStatus)
    {
        var job = CreateJobInState(initialStatus);
        
        var originalStatus = job.Status;
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        var originalErrorMessage = job.ErrorMessage;
        
        Assert.Throws<InvalidOperationException>(() => job.MarkFailed("Failed"));
        
        Assert.Equal(originalStatus, job.Status);
        Assert.Equal(originalUpdatedAtUtc, job.UpdatedAtUtc);
        Assert.Equal(originalErrorMessage, job.ErrorMessage);
    }
    
    [Fact]
    public void MarkFailed_WhenErrorMessageIsEmpty_ShouldThrow()
    {
        var job = CreateJobInState(JobStatus.Processing);
        var originalStatus = job.Status;
        var originalUpdatedAtUtc = job.UpdatedAtUtc;
        
        Assert.Throws<ArgumentException>(() => job.MarkFailed(""));
        Assert.Equal(originalStatus, job.Status);
        Assert.Equal(originalUpdatedAtUtc, job.UpdatedAtUtc);
    }
    
    private static DocumentJob CreateJobInState(JobStatus status)
    {
        var job = DocumentJob.Create("Test");

        switch (status)
        {
            case JobStatus.Pending:
                return job;
            case JobStatus.Queued:
                job.MarkQueued();
                return job;
            case JobStatus.Processing:
                job.MarkQueued();
                job.MarkProcessing();
                return job;
            case JobStatus.Completed:
                job.MarkQueued();
                job.MarkProcessing();
                job.MarkCompleted(ValidResult);
                return job;
            case JobStatus.Failed:
                job.MarkQueued();
                job.MarkProcessing();
                job.MarkFailed("Failed");
                return job;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
            
        }
    }
    
    private static readonly DocumentAnalysisResult ValidResult =
        new(
            WordCount: 1,
            CharacterCount: 1,
            LineCount: 1,
            KeywordHits: 1,
            Category: "Category",
            Summary: "Summary"
            );

    private static void AssertResultMatches(DocumentJob job, DocumentAnalysisResult expected)
    {
        var actual = new DocumentAnalysisResult(
            WordCount: job.WordCount!.Value,
            CharacterCount: job.CharacterCount!.Value,
            LineCount: job.LineCount!.Value,
            KeywordHits: job.KeywordHits!.Value,
            Category: job.Category!,
            Summary: job.Summary!);

        Assert.Equal(expected, actual);
    }
}