using DocumentProcessing.Application.DTOs;
using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Application.Messaging;
using DocumentProcessing.Application.Outbox;
using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Jobs;

public class JobService : IJobService
{
    private readonly IUnitOfWork _unitOfWork;

    public JobService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<JobResponseDto> CreateAsync(CreateJobRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var job = DocumentJob.Create(request.InputText);
        
        var jobMessage = new ProcessDocumentJobMessage
        {
            JobId = job.Id
        };

        var outboxMessage = OutboxMessageFactory.Create(jobMessage);
        
        job.MarkQueued();
        
        await _unitOfWork.JobRepository.AddAsync(job, cancellationToken);
        await _unitOfWork.OutboxRepository.AddAsync(outboxMessage, cancellationToken);
        
        await _unitOfWork.CommitAsync(cancellationToken);
        
        return MapToJobResponseDto(job);
    }

    public async Task<JobResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _unitOfWork.JobRepository.GetByIdAsync(id, cancellationToken);

        return job is null ? null : MapToJobResponseDto(job);
    }

    public async Task<IReadOnlyList<JobResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _unitOfWork.JobRepository.GetAllAsync(cancellationToken);
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