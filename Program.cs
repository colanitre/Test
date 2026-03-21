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
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RPG API V1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    // Enable Swagger in all environments for the web UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RPG API V1");
        c.RoutePrefix = "api";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RpgContext>();
    var creator = db.GetService<IRelationalDatabaseCreator>();

    // Check for database reset marker in development
    var resetMarkerPath = Path.Combine(Directory.GetCurrentDirectory(), "RESET_DB_MARKER");
    if (app.Environment.IsDevelopment() && File.Exists(resetMarkerPath))
    {
        try
        {
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
            File.Delete(resetMarkerPath);
        }
        catch
        {
            // Ignore errors during reset
        }
    }

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

    // Seed starter players/characters so the UI always has selectable data.
    if (!db.Players.Any())
    {
        var players = new List<Player>
        {
            new() { Username = "hero_one", Email = "hero_one@example.com" },
            new() { Username = "arcane_ace", Email = "arcane_ace@example.com" }
        };
        db.Players.AddRange(players);
        db.SaveChanges();

        var classMap = db.Classes
            .Include(c => c.Skills)
            .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

        var playerMap = db.Players
            .ToDictionary(p => p.Username, p => p, StringComparer.OrdinalIgnoreCase);

        var starterCharacters = new List<(string CharacterName, string ClassName, string Username, string Description)>
        {
            ("Thane", "Warrior", "hero_one", "A frontline fighter with strong defense."),
            ("Mira", "Archer", "hero_one", "A precise ranged attacker."),
            ("Selene", "Mage", "arcane_ace", "A spellcaster focused on elemental damage."),
            ("Nyx", "Rogue", "arcane_ace", "A swift striker that excels at critical hits.")
        };

        foreach (var (characterName, className, username, description) in starterCharacters)
        {
            if (!classMap.TryGetValue(className, out var playerClass) || !playerMap.TryGetValue(username, out var player))
            {
                continue;
            }

            var character = new Character(characterName, playerClass, player.Id)
            {
                Name = characterName,
                Description = description,
                Level = 1
            };

            db.Characters.Add(character);
        }

        db.SaveChanges();
    }

    // Seed all enemy classes that exist in the codebase.
    var enemyClassSeeds = new List<EnemyClass>
    {
        new Goblin(),
        new Orc(),
        new Dragon(),
        new Skeleton(),
        new DarkMage(),
        new Lich(),
        new Troll(),
        new ShadowAssassin(),
        new PhoenixGuardian()
    };

    var existingEnemyClassNames = db.EnemyClasses
        .Select(ec => ec.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var enemyClass in enemyClassSeeds)
    {
        if (!existingEnemyClassNames.Contains(enemyClass.Name))
        {
            db.EnemyClasses.Add(enemyClass);
        }
    }
    db.SaveChanges();

    var enemyClassMap = db.EnemyClasses
        .Include(ec => ec.Skills)
        .ToDictionary(ec => ec.Name, ec => ec, StringComparer.OrdinalIgnoreCase);

    // Seed enemies so each enemy class is represented in the UI dropdown.
    var enemySeeds = new List<(string Name, string EnemyClassName, int? Level, EnemyType? Type)>
    {
        ("Goblin Scout", "Goblin", null, null),
        ("Goblin Warrior", "Goblin", 2, null),
        ("Orc Warrior", "Orc", null, null),
        ("Orc Chieftain", "Orc", 5, null),
        ("Skeleton Warrior", "Skeleton", 2, null),
        ("Dark Mage Acolyte", "Dark Mage", 4, null),
        ("Troll Brute", "Troll", 6, null),
        ("Shadow Assassin", "Shadow Assassin", 5, null),
        ("Lich Overlord", "Lich", 100, EnemyType.Boss),
        ("Phoenix Guardian", "Phoenix Guardian", 100, EnemyType.Legendary),
        ("Ancient Red Dragon", "Ancient Dragon", 100, EnemyType.Legendary),
        ("Ancient Blue Dragon", "Ancient Dragon", 100, EnemyType.Legendary),
        ("Ancient Green Dragon", "Ancient Dragon", 100, EnemyType.Legendary),
        ("Ancient Black Dragon", "Ancient Dragon", 100, EnemyType.Legendary),
        ("Ancient White Dragon", "Ancient Dragon", 100, EnemyType.Legendary),
        ("Ancient Gold Dragon", "Ancient Dragon", 100, EnemyType.Legendary),
        ("Ancient Silver Dragon", "Ancient Dragon", 100, EnemyType.Legendary),
        ("Ancient Bronze Dragon", "Ancient Dragon", 100, EnemyType.Legendary),
        ("Ancient Copper Dragon", "Ancient Dragon", 100, EnemyType.Legendary),
        ("Legendary Brass Dragon", "Ancient Dragon", 100, EnemyType.Legendary)
    };

    var existingEnemyNames = db.Enemies
        .Select(e => e.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var (name, enemyClassName, level, type) in enemySeeds)
    {
        if (existingEnemyNames.Contains(name) || !enemyClassMap.TryGetValue(enemyClassName, out var enemyClass))
        {
            continue;
        }

        var enemy = new Enemy(name, enemyClass, level)
        {
            Name = name
        };

        if (type.HasValue)
        {
            enemy.Type = type.Value;
        }

        db.Enemies.Add(enemy);
    }

    // Keep hardest enemies at level 100 even in pre-existing databases.
    var hardestEnemies = db.Enemies
        .Where(e => e.Type == EnemyType.Boss || e.Type == EnemyType.Legendary)
        .ToList();

    foreach (var enemy in hardestEnemies)
    {
        if (enemy.Level != 100)
        {
            enemy.Level = 100;
        }
    }

    db.SaveChanges();
}

app.Run();
