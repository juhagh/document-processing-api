using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Domain.Entities;
using DocumentProcessing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly DocumentProcessingDbContext _context;
    
    public OutboxRepository(DocumentProcessingDbContext context)
    {
        _context = context;
    }
    
    public async Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }
    
    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(int batchSize, 
        CancellationToken cancellationToken = default)
    {
        return await _context.OutboxMessages
            .Where(o => o.PublishedOnUtc == null)
            .Where(o => o.AbandonedAtUtc == null)
            .OrderBy(o => o.CreatedAtUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}