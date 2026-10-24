using Microsoft.EntityFrameworkCore;
using MvcLoveMoney.Configs;
using MvcLoveMoney.Models;

namespace MvcLoveMoney.Data
{
    public class MvcLoveMoneyDbContext : DbContext
    {
        public MvcLoveMoneyDbContext(DbContextOptions<MvcLoveMoneyDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== Category =====
            modelBuilder.Entity<Category>(entity =>
            {
                // Loại danh mục chỉ được là 1 (Chi) hoặc 2 (Thu)
                entity.ToTable("Categories", t =>
                    t.HasCheckConstraint("CK_Categories_CategoryType", "[CategoryType] IN (1, 2)"));

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(MaxLengths.NameLength);

                entity.Property(e => e.Icon)
                      .IsRequired()
                      .HasMaxLength(MaxLengths.CategoryIcon)
                      .IsUnicode(false);

                entity.Property(e => e.ThemeColor)
                      .IsRequired()
                      .HasMaxLength(MaxLengths.ThemeColor)
                      .IsUnicode(false);

                // Không trùng tên trong cùng 1 loại
                entity.HasIndex(e => new { e.Name, e.CategoryType }).IsUnique();
            });

            // ===== Transaction =====
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.ToTable("Transactions", t =>
                {
                    t.HasCheckConstraint("CK_Transactions_Amount", "[Amount] > 0");
                    t.HasCheckConstraint("CK_Transactions_TransactionType", "[TransactionType] IN (1, 2)");
                });

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasMaxLength(MaxLengths.NameLength);

                entity.Property(e => e.Note)
                      .HasMaxLength(MaxLengths.NotesLength);

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.Transactions)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
