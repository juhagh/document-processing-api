using DocumentProcessing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DocumentProcessing.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        // 1. Table mapping
        builder.ToTable("outbox_messages", "public");
        
        // 2. Primary key
        builder.HasKey(o => o.Id);
        
        // 3. Property configurations
        // Required
        builder.Property(o => o.Content).IsRequired();
        builder.Property(o => o.Type).IsRequired().HasMaxLength(255);
        builder.Property(o => o.CreatedAtUtc).IsRequired();
        
        // Optional
        builder.Property(o => o.PublishedOnUtc);
        builder.Property(o => o.ErrorMessage).HasMaxLength(1024);

        // 4. Indexes
        builder.HasIndex(o => o.CreatedAtUtc)
            .HasFilter("\"PublishedOnUtc\" IS NULL AND \"ErrorMessage\" IS NULL")
            .HasDatabaseName("idx_outbox_messages_unpublished_createdat");
    }
    
}