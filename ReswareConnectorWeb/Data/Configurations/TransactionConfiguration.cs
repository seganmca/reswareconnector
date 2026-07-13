using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ReswareConnectorWeb.Data.Entities;

namespace ReswareConnectorWeb.Data.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            // Table name
            builder.ToTable("Transactions");

            // Primary Key
            builder.HasKey(t => t.TransactionId);

            // Properties configuration
            builder.Property(t => t.TransactionId)
                .ValueGeneratedOnAdd()
                .HasColumnName("transaction_id")
                .IsRequired();

            builder.Property(t => t.TransactionTypeId)
                .HasColumnName("transaction_type_id")
                .IsRequired();

            builder.Property(t => t.FileNumber)
                .HasColumnName("file_number")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(t => t.DataPath)
                .HasColumnName("data_path")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(t => t.ReceivedTime)
                .HasColumnName("received_time")
                .IsRequired();

            builder.Property(t => t.Processed)
                .HasColumnName("processed")
                .IsRequired()
                .HasDefaultValue(false);

            // Indexes
            builder.HasIndex(t => t.FileNumber)
                .HasDatabaseName("idx_transactions_file_number");

            builder.HasIndex(t => t.ReceivedTime)
                .HasDatabaseName("idx_transactions_received_time");

            builder.HasIndex(t => t.Processed)
                .HasDatabaseName("idx_transactions_processed");

            builder.HasIndex(t => t.TransactionTypeId)
                .HasDatabaseName("idx_transactions_type_id");

            // Relationships
            builder.HasMany(t => t.Items)
                .WithOne(ti => ti.Transaction)
                .HasForeignKey(ti => ti.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
