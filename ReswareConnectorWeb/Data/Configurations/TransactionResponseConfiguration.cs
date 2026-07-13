using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using ReswareConnectorWeb.Data.Entities;

namespace ReswareConnectorWeb.Data.Configurations
{
    public class TransactionResponseConfiguration : IEntityTypeConfiguration<TransactionResponse>
    {
        public void Configure(EntityTypeBuilder<TransactionResponse> builder)
        {
            // Table name
            builder.ToTable("TransactionResponses");

            // Primary Key
            builder.HasKey(tr => tr.TransactionResponseId);

            // Properties configuration
            builder.Property(tr => tr.TransactionResponseId)
                .ValueGeneratedOnAdd()
                .HasColumnName("transaction_response_id")
                .IsRequired();

            builder.Property(tr => tr.TransactionItemId)
                .HasColumnName("transaction_item_id")
                .IsRequired();

            builder.Property(tr => tr.ReceivedTime)
                .HasColumnName("received_time")
                .IsRequired();

            builder.Property(tr => tr.ResponseCode)
                .HasColumnName("response_code")
                .IsRequired();

            builder.Property(tr => tr.ResponseMessage)
                .HasColumnName("response_message")
                .HasMaxLength(1000);

            // Indexes
            builder.HasIndex(tr => tr.TransactionItemId)
                .HasDatabaseName("idx_transaction_responses_item_id");

            builder.HasIndex(tr => tr.ReceivedTime)
                .HasDatabaseName("idx_transaction_responses_received_time");

            builder.HasIndex(tr => tr.ResponseCode)
                .HasDatabaseName("idx_transaction_responses_code");

            // Relationships
            builder.HasOne(tr => tr.TransactionItem)
                .WithMany(ti => ti.Responses)
                .HasForeignKey(tr => tr.TransactionItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
