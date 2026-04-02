using DocumentProcessing.Domain.ValueObjects;

namespace DocumentProcessing.Worker.Services;

public interface IDocumentAnalysisService
{
    DocumentAnalysisResult Analyze(string inputText);
}