using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Interfaces;

public interface IJobRepository
{
    Task AddAsync(DocumentJob job, CancellationToken cancellationToken = default);
    Task<DocumentJob?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentJob>> GetAllAsync(CancellationToken cancellationToken = default);
}