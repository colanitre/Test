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
    public DbSet<Skill> Skills { get; set; }
    public DbSet<Enemy> Enemies { get; set; }
    public DbSet<EnemyClass> EnemyClasses { get; set; }
    public DbSet<FightSession> FightSessions { get; set; }
    public DbSet<FightMove> FightMoves { get; set; }
    public DbSet<CharacterLoadout> CharacterLoadouts { get; set; }
    public DbSet<ProgressionEvent> ProgressionEvents { get; set; }
    public DbSet<RogueliteRun> RogueliteRuns { get; set; }
    public DbSet<TalentNode> TalentNodes { get; set; }
    public DbSet<CharacterEquipment> CharacterEquipments { get; set; }

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
            .WithOne(c => c.Class)
            .HasForeignKey(c => c.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure many-to-many Class - Skill relationship
        modelBuilder.Entity<Class>()
            .HasMany(c => c.Skills)
            .WithMany(s => s.Classes);

        // Configure EnemyClass - Enemy relationship
        modelBuilder.Entity<EnemyClass>()
            .HasMany(ec => ec.Enemies)
            .WithOne(e => e.EnemyClass)
            .HasForeignKey(e => e.EnemyClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure many-to-many EnemyClass - Skill relationship
        modelBuilder.Entity<EnemyClass>()
            .HasMany(ec => ec.Skills)
            .WithMany(s => s.EnemyClasses);
            
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

        modelBuilder.Entity<FightSession>()
            .HasMany(fs => fs.Moves)
            .WithOne(m => m.FightSession)
            .HasForeignKey(m => m.FightSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<FightSession>()
            .HasIndex(fs => fs.Status);

        modelBuilder.Entity<FightMove>()
            .HasIndex(m => m.FightSessionId);

        modelBuilder.Entity<CharacterLoadout>()
            .HasIndex(l => new { l.PlayerId, l.CharacterId });

        modelBuilder.Entity<ProgressionEvent>()
            .HasIndex(e => e.Timestamp);

        modelBuilder.Entity<RogueliteRun>()
            .HasIndex(r => new { r.PlayerId, r.CharacterId });

        modelBuilder.Entity<TalentNode>()
            .HasIndex(t => t.ClassName);

        modelBuilder.Entity<CharacterEquipment>()
            .HasKey(e => e.CharacterId);

    }
}
