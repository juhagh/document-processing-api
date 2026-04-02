using DocumentProcessing.Domain.ValueObjects;

namespace DocumentProcessing.Worker.Services;

public class DocumentAnalysisService : IDocumentAnalysisService
{
    public DocumentAnalysisResult Analyze(string inputText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputText);
        
        var characterCount = inputText.Length;
        var normalized = inputText
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .TrimEnd('\n');

        var lineCount = normalized.Length == 0
            ? 0
            : normalized.Split('\n').Length;
        
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