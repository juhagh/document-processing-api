namespace DocumentProcessing.Domain.ValueObjects;

public record DocumentAnalysisResult(
    int? WordCount,
    int? CharacterCount,
    int? LineCount,
    int? KeywordHits,
    string? Category,
    string? Summary
);