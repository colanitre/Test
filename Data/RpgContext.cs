using Microsoft.EntityFrameworkCore;
using RpgApi.Models;

namespace RpgApi.Data;

public class RpgContext : DbContext
{
    public RpgContext(DbContextOptions<RpgContext> options) : base(options)
    {
    }

    public DbSet<Player> Players { get; set; }
    public DbSet<Character> Characters { get; set; }
    public DbSet<Class> Classes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Player - Character relationship
        modelBuilder.Entity<Player>()
            .HasMany(p => p.Characters)
            .WithOne(c => c.Player)
            .HasForeignKey(c => c.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure Class - Character relationship
        modelBuilder.Entity<Class>()
            .HasMany(c => c.Characters)
            .WithOne(ch => ch.Class)
            .HasForeignKey(ch => ch.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure indexes
        modelBuilder.Entity<Player>()
            .HasIndex(p => p.Username)
            .IsUnique();

        modelBuilder.Entity<Player>()
            .HasIndex(p => p.Email)
            .IsUnique();

        modelBuilder.Entity<Character>()
            .HasIndex(c => c.PlayerId);

        modelBuilder.Entity<Character>()
            .HasIndex(c => c.ClassId);
    }
}
