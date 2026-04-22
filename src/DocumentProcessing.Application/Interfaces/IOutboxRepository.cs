using DocumentProcessing.Domain.Entities;

namespace DocumentProcessing.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}