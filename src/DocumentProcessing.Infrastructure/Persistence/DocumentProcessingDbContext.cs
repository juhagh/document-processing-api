using DocumentProcessing.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DocumentProcessing.Infrastructure.Persistence;

public class DocumentProcessingDbContext : DbContext
{
    public DocumentProcessingDbContext(DbContextOptions<DocumentProcessingDbContext> options) : base(options)
    {
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentProcessingDbContext).Assembly);
    }
    
    public DbSet<DocumentJob> DocumentJobs => Set<DocumentJob>();
}