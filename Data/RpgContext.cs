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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Player - Character relationship
        modelBuilder.Entity<Player>()
            .HasMany(p => p.Characters)
            .WithOne(c => c.Player)
            .HasForeignKey(c => c.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure indexes
        modelBuilder.Entity<Player>()
            .HasIndex(p => p.Username)
            .IsUnique();

        modelBuilder.Entity<Player>()
            .HasIndex(p => p.Email)
            .IsUnique();

        modelBuilder.Entity<Character>()
            .HasIndex(c => c.PlayerId);
    }
}
