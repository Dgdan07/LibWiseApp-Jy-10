using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LibWiseApp.Models;

namespace LibWiseApp.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Book> Books { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Borrower> Borrowers { get; set; }
    public DbSet<BorrowingRecord> BorrowingRecords { get; set; }
    public DbSet<Fine> Fines { get; set; }
    public DbSet<BookStatusLog> BookStatusLogs { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<FineRule> FineRules { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Borrower>()
            .HasIndex(b => b.Barcode)
            .IsUnique();

        builder.Entity<BorrowingRecord>()
            .HasOne(br => br.BorrowedBy)
            .WithMany()
            .HasForeignKey(br => br.BorrowedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<BorrowingRecord>()
            .HasOne(br => br.ReturnedBy)
            .WithMany()
            .HasForeignKey(br => br.ReturnedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<Fine>()
            .HasOne(f => f.PaidBy)
            .WithMany()
            .HasForeignKey(f => f.PaidByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<BookStatusLog>()
            .HasOne(l => l.ChangedBy)
            .WithMany()
            .HasForeignKey(l => l.ChangedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Entity<SystemConfig>()
            .HasIndex(c => c.Key)
            .IsUnique();
    }
}
