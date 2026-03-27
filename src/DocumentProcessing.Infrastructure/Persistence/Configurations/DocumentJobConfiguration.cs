using DocumentProcessing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentProcessing.Infrastructure.Persistence.Configurations;

public class DocumentJobConfiguration : IEntityTypeConfiguration<DocumentJob>
{
    public void Configure(EntityTypeBuilder<DocumentJob> builder)
    {
        // 1. Table mapping
        builder.ToTable("document_jobs", "public");
        
        // 2. Primary key
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedOnAdd();
        
        // 3. Property configurations
        // Required
        builder.Property(j => j.InputText).IsRequired();
        builder.Property(j => j.Status).IsRequired();
        builder.Property(j => j.SubmittedAtUtc).IsRequired();
        builder.Property(j => j.UpdatedAtUtc).IsRequired();
        // Optional
        builder.Property(j => j.ErrorMessage).HasMaxLength(2000);
        builder.Property(j => j.Category).HasMaxLength(200);
        builder.Property(j => j.Summary).HasMaxLength(4000);
        
        // 4. Enum mappings
        builder.Property(j => j.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);
        
        // 5. Indexes
        builder.HasIndex(j => j.Status)
            .HasDatabaseName("idx_document_jobs_status");
        
    }
}