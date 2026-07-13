using Microsoft.EntityFrameworkCore;
using ReswareConnectorWeb.Data.Entities;
using System.Diagnostics;
using System.Reflection;

namespace ReswareConnectorWeb.Data
{
    public class ReswareConnectorDbContext : DbContext
    {
        public ReswareConnectorDbContext(DbContextOptions<ReswareConnectorDbContext> options)
            : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<TransactionItem> TransactionItems { get; set; }
        public DbSet<TransactionResponse> TransactionResponses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
