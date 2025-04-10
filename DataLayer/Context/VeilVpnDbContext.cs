using Domain.Entities;
using Domain.Entities.Account;
using Domain.Entities.VPN;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<Tutorial> Tutorials { get; set; } // اضافه کردن این خط
        public DbSet<DiscountCode> DiscountCodes { get; set; }
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
                .Property(i => i.PlanDiscountAmount)
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

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                // رابطه با کاربر فرستنده
                entity.HasOne(d => d.SenderUser) // فرض: پراپرتی ناوبری به نام SenderUser دارید
                      .WithMany() // اگر در User پراپرتی Collection برای پیام‌های ارسالی ندارید
                                  // .WithMany(p => p.SentMessages) // اگر پراپرتی Collection دارید
                      .HasForeignKey(d => d.SenderUserId)
                      .OnDelete(DeleteBehavior.Restrict); // <--- تغییر کلیدی: جلوگیری از Cascade

                // رابطه با کاربر گیرنده
                entity.HasOne(d => d.RecipientUser) // فرض: پراپرتی ناوبری به نام RecipientUser دارید
                      .WithMany() // اگر در User پراپرتی Collection برای پیام‌های دریافتی ندارید
                                  // .WithMany(p => p.ReceivedMessages) // اگر پراپرتی Collection دارید
                      .HasForeignKey(d => d.RecipientUserId)
                      .OnDelete(DeleteBehavior.Restrict); // <--- تغییر کلیدی: جلوگیری از Cascade

                // رابطه برای ریپلای (که قبلاً احتمالاً تنظیم شده)
                // اطمینان حاصل کنید که این هم Cascade نیست، معمولاً Restrict مناسب است
                entity.HasOne(m => m.ReplyToMessage)
                      .WithMany(m => m.Replies)
                      .HasForeignKey(m => m.ReplyToMessageId)
                      .OnDelete(DeleteBehavior.Restrict); // یا .SetNull بسته به نیاز

                // سایر پیکربندی‌های احتمالی برای ChatMessage...
                // entity.Property(e => e.SenderName).IsRequired(); // مثال
            });
        }

    }
}