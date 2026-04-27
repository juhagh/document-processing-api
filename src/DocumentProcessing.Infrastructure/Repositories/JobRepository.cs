using DocumentProcessing.Application.Interfaces;
using DocumentProcessing.Domain.Entities;
using DocumentProcessing.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Infrastructure.Repositories;

public class JobRepository : IJobRepository
{
    private readonly DocumentProcessingDbContext _context;

    public JobRepository(DocumentProcessingDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(DocumentJob job, CancellationToken cancellationToken = default)
    {
        await _context.DocumentJobs.AddAsync(job, cancellationToken);
    }

    public async Task<DocumentJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DocumentJobs
            .AsNoTracking()
            .OrderByDescending(j => j.SubmittedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<DocumentJob?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentJobs
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }
}