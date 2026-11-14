using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

namespace TodoApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<ReadingSource> ReadingSources => Set<ReadingSource>();
    public DbSet<ReadingDocument> ReadingDocuments => Set<ReadingDocument>();
    public DbSet<ReadingProgress> ReadingProgresses => Set<ReadingProgress>();
    public DbSet<Highlight> Highlights => Set<Highlight>();
    public DbSet<ReadingNote> ReadingNotes => Set<ReadingNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<TodoItem>()
            .Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<TeamMember>()
            .HasKey(tm => new { tm.TeamId, tm.UserId });

        modelBuilder.Entity<TeamMember>()
            .Property(tm => tm.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        modelBuilder.Entity<TeamMember>()
            .HasOne(tm => tm.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeamMember>()
            .HasOne(tm => tm.User)
            .WithMany(u => u.TeamMemberships)
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReadingSource>()
            .HasIndex(s => s.Slug).IsUnique();

        modelBuilder.Entity<ReadingDocument>()
            .HasIndex(d => new { d.SourceId, d.Slug }).IsUnique();

        modelBuilder.Entity<ReadingProgress>()
            .HasKey(p => new { p.UserId, p.DocumentId });

        modelBuilder.Entity<ReadingProgress>()
            .HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ReadingProgress>()
            .HasOne(p => p.Document).WithMany(d => d.Progresses).HasForeignKey(p => p.DocumentId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Highlight>()
            .HasOne(h => h.User).WithMany().HasForeignKey(h => h.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Highlight>()
            .HasOne(h => h.Document).WithMany(d => d.Highlights).HasForeignKey(h => h.DocumentId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReadingNote>()
            .HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ReadingNote>()
            .HasOne(n => n.Document).WithMany(d => d.Notes).HasForeignKey(n => n.DocumentId).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Team>()
            .HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<TodoItem>()
            .HasOne(t => t.Team)
            .WithMany(team => team.Tasks)
            .HasForeignKey(t => t.TeamId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        modelBuilder.Entity<TodoItem>()
            .HasOne(t => t.CreatedBy)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        modelBuilder.Entity<TodoItem>()
            .HasOne(t => t.AssignedTo)
            .WithMany()
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
