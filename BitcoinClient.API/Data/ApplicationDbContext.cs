using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BitcoinClient.API.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
         
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            var precision = "decimal(9, 8)";
            builder.Entity<Wallet>()
                .Property(p => p.Balance)
                .HasColumnType(precision);
            builder.Entity<InputTransaction>()
                .Property(p => p.Amount)
                .HasColumnType(precision);
            builder.Entity<InputTransaction>()
                .Property(p => p.Fee)
                .HasColumnType(precision);
            builder.Entity<InputTransaction>()
                .HasAlternateKey("TxId", "AddressId");

            builder.Entity<OutputTransaction>()
                .Property(p => p.Amount)
                .HasColumnType(precision);
            builder.Entity<OutputTransaction>()
                .Property(p => p.Fee)
                .HasColumnType(precision);

            base.OnModelCreating(builder);
        }

        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<InputTransaction> InputTransactions { get; set; }
        public DbSet<OutputTransaction> OutputTransactions { get; set; }
    }
}
