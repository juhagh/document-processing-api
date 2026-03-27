namespace DocumentProcessing.Api.Contracts;

public class CreateJobRequest
{
    public required string InputText { get; init; }
}