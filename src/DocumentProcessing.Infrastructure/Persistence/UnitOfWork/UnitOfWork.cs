using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Infrastructure.Repositories;

namespace DocumentProcessing.Infrastructure.Persistence.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly DocumentProcessingDbContext _context;
    
    public IJobRepository JobRepository { get; private set; }
    public IOutboxRepository OutboxRepository { get; private set; }

    public UnitOfWork(DocumentProcessingDbContext context, IJobRepository jobRepository, IOutboxRepository outboxRepository)
    {
        _context = context;
        JobRepository = jobRepository;
        OutboxRepository = outboxRepository;
    }
    
    public async Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}