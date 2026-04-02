using DocumentProcessing.Worker.Services;

namespace DocumentProcessing.Worker.Tests.Services;

public class DocumentAnalysisServiceTests
{
    private readonly DocumentAnalysisService _analysisService = new();
    
    [Fact]
    public void Analyze_WithSingleLineInput_ReturnsExpectedCounts()
    {
        var inputText = "This is a one line test.";
        var result = _analysisService.Analyze(inputText);
        Assert.Equal(1, result.LineCount);
        Assert.Equal(6, result.WordCount);
        Assert.Equal(24, result.CharacterCount);
        Assert.NotNull(result.Summary);
        Assert.Equal(inputText, result.Summary);
        Assert.Equal("General", result.Category);
    }

    [Fact]
    public void Analyze_WithMultilineInput_ReturnsExpectedCounts()
    {
        var inputText = "This is a \ntwo line test.";
        var result = _analysisService.Analyze(inputText);
        Assert.Equal(2, result.LineCount);
        Assert.Equal(6, result.WordCount);
        Assert.Equal(25, result.CharacterCount);
        Assert.NotNull(result.Summary);
        Assert.Equal(inputText, result.Summary);
        Assert.Equal("General", result.Category);
    }

    [Fact]
    public void Analyze_WithTrailingNewline_DoesNotCountExtraLine()
    {
        var inputText = "This is a one line test with trailing new line.\n";
        var result = _analysisService.Analyze(inputText);
        Assert.Equal(1, result.LineCount);
        Assert.Equal(10, result.WordCount);
        Assert.Equal(48, result.CharacterCount);
        Assert.NotNull(result.Summary);
        Assert.Equal(inputText, result.Summary);
        Assert.Equal("General", result.Category);
        
    }

    [Fact]
    public void Analyze_WithLongInput_TruncatesSummary()
    {
        var inputText =
            "This is a deliberately long input text used to verify that the summary logic truncates content correctly after one hundred and twenty characters.\nIt also includes a second line for a slightly more realistic test case.";
        
        var result = _analysisService.Analyze(inputText);
        
        Assert.NotNull(result.Summary);
        Assert.EndsWith("...", result.Summary);
        Assert.True(result.Summary.Length < inputText.Length);
        Assert.True(result.Summary.Length <= 123);
    }

    [Fact]
    public void Analyze_WithWindowsLineEndings_ReturnsExpectedLineCount()
    {
        var inputText = "Line one\r\nLine two\r\nLine three\r\n";
        var result = _analysisService.Analyze(inputText);
        Assert.Equal(3, result.LineCount);
        Assert.Equal(6, result.WordCount);
        Assert.Equal(32, result.CharacterCount);
        Assert.NotNull(result.Summary);
        Assert.Equal(inputText, result.Summary);
        Assert.Equal("General", result.Category);
    }

    [Fact]
    public void Analyze_WithTriggerFailure_ThrowsInvalidOperationException()
    {
        var inputText = "TRIGGER_FAILURE";

        var ex = Assert.Throws<InvalidOperationException>(() => _analysisService.Analyze(inputText));
        Assert.Equal("Simulated failure for testing purposes.", ex.Message);
    }
}