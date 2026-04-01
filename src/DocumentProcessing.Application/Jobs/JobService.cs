using DocumentProcessing.Application.DTOs;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Messaging;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Jobs;

public class JobService : IJobService
{
    
    private readonly IJobRepository _repository;
    private readonly IJobMessagePublisher _publisher;

    public JobService(IJobRepository repository, IJobMessagePublisher publisher)
    {
        _repository = repository;
        _publisher = publisher;
    }
    
    public async Task<JobResponseDto> CreateAsync(CreateJobRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var job = DocumentJob.Create(request.InputText);
        await _repository.AddAsync(job, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        
        var message = new ProcessDocumentJobMessage
        {
            JobId = job.Id
        };

        // In v1, mark the job as Queued before publishing to avoid a race where the worker
        // consumes the message before the queued state is persisted.
        // TODO: Replace this with an outbox pattern for stronger consistency guarantees.
        job.MarkQueued();
        await _repository.SaveChangesAsync(cancellationToken);
        
        await _publisher.PublishAsync(message, cancellationToken);
        
        return MapToJobResponseDto(job);
        
    }

    public async Task<JobResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var job = await _repository.GetByIdAsync(id, cancellationToken);

        return job is null ? null : MapToJobResponseDto(job);
    }

    public async Task<IReadOnlyList<JobResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _repository.GetAllAsync(cancellationToken);
        var jobList = jobs
            .Select(MapToJobResponseDto)
            .ToList()
            .AsReadOnly();

        return jobList;
    }

    private static JobResponseDto MapToJobResponseDto(DocumentJob job)
    {
        return new JobResponseDto
        {
            Id = job.Id,
            Status = job.Status,
            InputText = job.InputText,
            SubmittedAtUtc = job.SubmittedAtUtc,
            UpdatedAtUtc = job.UpdatedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc,
            ErrorMessage = job.ErrorMessage,
            WordCount = job.WordCount,
            CharacterCount = job.CharacterCount,
            LineCount = job.LineCount,
            KeywordHits = job.KeywordHits,
            Category = job.Category,
            Summary = job.Summary,
        };
    }
}