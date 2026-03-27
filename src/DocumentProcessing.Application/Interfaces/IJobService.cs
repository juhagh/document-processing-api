using DocumentProcessing.Application.DTOs;

namespace DocumentProcessing.Application.Interfaces;

public interface IJobService
{
    Task<JobResponseDto> CreateAsync(CreateJobRequestDto request, CancellationToken cancellationToken = default);
    Task<JobResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}