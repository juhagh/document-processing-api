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
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DocumentJob?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

}