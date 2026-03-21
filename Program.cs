using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using RpgApi.Data;
using RpgApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.Configure<DamageCurveConfig>(builder.Configuration.GetSection("DamageCurveConfig"));

// Add Entity Framework Core with SQLite
builder.Services.AddDbContext<RpgContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=RpgDatabase.db";
    options.UseSqlite(connectionString);
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", corsPolicyBuilder =>
    {
        corsPolicyBuilder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RpgContext>();
    var creator = db.GetService<IRelationalDatabaseCreator>();

    // Ensure database exists and migrations/created tables are present.
    // On schema mismatch in dev, recreate automatically.
    if (!creator.HasTables())
    {
        db.Database.EnsureCreated();
    }
    else
    {
        var hasEnemyClasses = false;
        try
        {
            hasEnemyClasses = db.EnemyClasses.Any();
        }
        catch (SqliteException)
        {
            hasEnemyClasses = false;
        }

        if (!hasEnemyClasses)
        {
            if (app.Environment.IsDevelopment())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }
            else
            {
                throw new InvalidOperationException("Database schema is incompatible: missing EnemyClasses table.");
            }
        }
    }

    // Seed character classes if not already present
    if (!db.Classes.Any())
    {
        var classes = new List<RpgApi.Models.Class>
        {
            new RpgApi.Models.Mage(),
            new RpgApi.Models.Warrior(),
            new RpgApi.Models.Archer(),
            new RpgApi.Models.Rogue()
        };

        db.Classes.AddRange(classes);
        db.SaveChanges();
    }

    // Seed enemy classes if not already present
    if (!db.EnemyClasses.Any())
    {
        var enemyClasses = new List<EnemyClass>
        {
            new Goblin(),
            new Orc(),
            new Dragon()
        };

        db.EnemyClasses.AddRange(enemyClasses);
        db.SaveChanges();
    }

    // Seed enemies if not already present
    if (!db.Enemies.Any())
    {
        var goblinClass = db.EnemyClasses.First(ec => ec.Name == "Goblin");
        var orcClass = db.EnemyClasses.First(ec => ec.Name == "Orc");
        var dragonClass = db.EnemyClasses.First(ec => ec.Name == "Ancient Dragon");

        var enemies = new List<Enemy>
        {
            new Enemy("Goblin Scout", goblinClass) { Name = "Goblin Scout" },
            new Enemy("Goblin Warrior", goblinClass) { Name = "Goblin Warrior", Level = 2 },
            new Enemy("Orc Warrior", orcClass) { Name = "Orc Warrior" },
            new Enemy("Orc Chieftain", orcClass) { Name = "Orc Chieftain", Level = 5 },
            new Enemy("Ancient Red Dragon", dragonClass) { Name = "Ancient Red Dragon" }
        };

        db.Enemies.AddRange(enemies);
        db.SaveChanges();
    }
}

app.Run();
