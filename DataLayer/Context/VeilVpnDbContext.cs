using Domain.Entities;
using Domain.Entities.Account;
using Domain.Entities.VPN;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DataLayer.Context
{
    public class VeilVpnDbContext : DbContext
    {
        public VeilVpnDbContext(DbContextOptions<VeilVpnDbContext> options) : base(options)
        {
        }

        // DBSets
        public DbSet<User> Users { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<VPNServer> VPNServers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تنظیمات مدل فاکتور
            modelBuilder.Entity<Invoice>()
                .HasIndex(i => i.InvoiceNumber)
                .IsUnique();

            modelBuilder.Entity<Invoice>()
                .Property(i => i.BasePrice)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.DiscountAmount)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.FinalPrice)
                .HasColumnType("decimal(18,2)");

            // رابطه User و Subscription
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.User)
                .WithMany(u => u.Subscriptions)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // رابطه User و Invoice
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.User)
                .WithMany(u => u.Invoices)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // رابطه Invoice و Subscription (یک به یک)
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Invoice)
                .WithOne(i => i.Subscription)
                .HasForeignKey<Subscription>(s => s.InvoiceId)
                .OnDelete(DeleteBehavior.NoAction);

            // تنظیمات مدل VPNSubscription
            modelBuilder.Entity<VPNSubscription>()
                .HasOne(vs => vs.VPNServer)
                .WithMany()
                .HasForeignKey(vs => vs.VPNServerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VPNSubscription>()
                .HasOne(vs => vs.User)
                .WithMany()
                .HasForeignKey(vs => vs.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }

    }
}