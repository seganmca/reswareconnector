using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ReswareConnectorWeb.Data.Entities;

namespace ReswareConnectorWeb.Data.Configurations
{
    public class TransactionItemConfiguration : IEntityTypeConfiguration<TransactionItem>
    {
        public void Configure(EntityTypeBuilder<TransactionItem> builder)
        {
            // Table name
            builder.ToTable("TransactionItems");

            // Primary Key
            builder.HasKey(ti => ti.TransactionItemId);

            // Properties configuration
            builder.Property(ti => ti.TransactionItemId)
                .ValueGeneratedOnAdd()
                .HasColumnName("transaction_item_id")
                .IsRequired();

            builder.Property(ti => ti.TransactionId)
                .HasColumnName("transaction_id")
                .IsRequired();

            builder.Property(ti => ti.TransactionTypeId)
                .HasColumnName("transaction_type_id")
                .IsRequired();

            builder.Property(ti => ti.Processed)
                .HasColumnName("processed")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(ti => ti.RetryCount)
                .HasColumnName("retry_count")
                .HasColumnType("TINYINT UNSIGNED");

            builder.Property(ti => ti.ResponseSent)
                .HasColumnName("response_sent")
                .HasColumnType("BIT(1)");

            builder.Property(ti => ti.LastUpdatedTime)
                .HasColumnName("last_updated_time");

            // Indexes
            builder.HasIndex(ti => ti.TransactionId)
                .HasDatabaseName("idx_transaction_items_transaction_id");

            builder.HasIndex(ti => ti.TransactionTypeId)
                .HasDatabaseName("idx_transaction_items_type_id");

            builder.HasIndex(ti => ti.Processed)
                .HasDatabaseName("idx_transaction_items_processed");

            builder.HasIndex(ti => new { ti.TransactionId, ti.Processed })
                .HasDatabaseName("idx_transaction_items_transaction_processed");

            // Relationships
            builder.HasOne(ti => ti.Transaction)
                .WithMany(t => t.Items)
                .HasForeignKey(ti => ti.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(ti => ti.Responses)
                .WithOne(tr => tr.TransactionItem)
                .HasForeignKey(tr => tr.TransactionItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
