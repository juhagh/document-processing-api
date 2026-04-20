using DocumentProcessing.Domain.Enums;

namespace DocumentProcessing.Application.DTOs;

public class JobResponseDto
{
    public Guid Id { get; init; }
    public JobStatus Status { get; init; }
    public required string InputText { get; init; }
    public DateTime SubmittedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? ErrorMessage { get; init; }
    public int? WordCount { get; init; }
    public int? CharacterCount { get; init; }
    public int? LineCount { get; init; }
    public int? KeywordHits { get; init; }
    public string? Category { get; init; }
    public string? Summary { get; init; }
}