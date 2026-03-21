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

        var hasClassProgressionColumns = true;
        try
        {
            _ = db.Classes.Select(c => new { c.Archetype, c.Branch }).FirstOrDefault();
        }
        catch (SqliteException)
        {
            hasClassProgressionColumns = false;
        }

        if (!hasClassProgressionColumns)
        {
            if (app.Environment.IsDevelopment())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }
            else
            {
                throw new InvalidOperationException("Database schema is incompatible: missing class progression columns.");
            }
        }

        var hasCharacterProgressionColumns = true;
        try
        {
            _ = db.Characters.Select(c => c.AdvancedPath).FirstOrDefault();
        }
        catch (SqliteException)
        {
            hasCharacterProgressionColumns = false;
        }

        if (!hasCharacterProgressionColumns)
        {
            if (app.Environment.IsDevelopment())
            {
                db.Database.EnsureDeleted();
                db.Database.EnsureCreated();
            }
            else
            {
                throw new InvalidOperationException("Database schema is incompatible: missing character progression columns.");
            }
        }
    }

    void MergeMissingSkills(RpgApi.Models.Class targetClass, IEnumerable<Skill> seededSkills)
    {
        var existingSkillNames = targetClass.Skills
            .Select(s => s.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var seededSkill in seededSkills)
        {
            if (existingSkillNames.Contains(seededSkill.Name))
            {
                continue;
            }

            targetClass.Skills.Add(seededSkill);
            existingSkillNames.Add(seededSkill.Name);
        }
    }

    // Seed/update base character classes.
    var baseClassSeeds = new List<RpgApi.Models.Class>
    {
        new RpgApi.Models.Mage(),
        new RpgApi.Models.Warrior(),
        new RpgApi.Models.Archer(),
        new RpgApi.Models.Rogue()
    };

    var existingClassesByName = db.Classes
        .Include(c => c.Skills)
        .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

    foreach (var seed in baseClassSeeds)
    {
        if (!existingClassesByName.TryGetValue(seed.Name, out var existingBaseClass))
        {
            db.Classes.Add(seed);
            continue;
        }

        existingBaseClass.Archetype = seed.Archetype;
        existingBaseClass.Tier = seed.Tier;
        existingBaseClass.RequiredLevel = seed.RequiredLevel;
        existingBaseClass.IsAdvanced = seed.IsAdvanced;
        existingBaseClass.Branch = seed.Branch;

        MergeMissingSkills(existingBaseClass, seed.Skills);
    }

    db.SaveChanges();

    // Seed advanced classes every 50 levels, with three specialization choices per archetype.
    var advancedClassSpecs = new Dictionary<string, string[][]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Warrior"] =
        [
            ["Blademaster", "Warlord", "Juggernaut"],
            ["Dreadnought", "Storm Champion", "Colossus Knight"],
            ["Aegis Paragon", "War Saint", "Titan Vanguard"],
            ["Godslayer", "Eternal Bastion", "Worldbreaker"]
        ],
        ["Archer"] =
        [
            ["Sharpshot", "Windrunner", "Beast Hunter"],
            ["Storm Ranger", "Hawkeye", "Night Falcon"],
            ["Celestial Sniper", "Starstrider", "Sky Reaver"],
            ["Heavenpiercer", "Void Marksman", "Cosmos Tracker"]
        ],
        ["Rogue"] =
        [
            ["Shadowblade", "Poisonmaster", "Trickster"],
            ["Night Reaper", "Venom Wraith", "Phantom Duelist"],
            ["Eclipse Stalker", "Abyss Dancer", "Silent Judicator"],
            ["Void Assassin", "Dread Mirage", "Fate Ripper"]
        ],
        ["Mage"] =
        [
            ["Pyromancer", "Cryomancer", "Stormcaller"],
            ["Archmage", "Spellweaver", "Runelord"],
            ["Astral Sage", "Chronomancer", "Ethereal Arcanist"],
            ["Worldshaper", "Infinity Magus", "Deific Sorcerer"]
        ]
    };

    var tierNames = new[] { "Mythical", "Legendary", "Divine", "Deific" };
    var tierRequiredLevels = new[] { 50, 100, 150, 200 };
    var archetypeElements = new Dictionary<string, ElementType>(StringComparer.OrdinalIgnoreCase)
    {
        ["Warrior"] = ElementType.Physical,
        ["Archer"] = ElementType.Poison,
        ["Rogue"] = ElementType.Shadow,
        ["Mage"] = ElementType.Arcane
    };

    var advancedSeeds = new List<RpgApi.Models.Class>();

    foreach (var (archetype, tiers) in advancedClassSpecs)
    {
        for (var tierIndex = 0; tierIndex < tiers.Length; tierIndex++)
        {
            var tier = tierIndex + 1;
            var tierName = tierNames[tierIndex];
            var requiredLevel = tierRequiredLevels[tierIndex];
            var primaryElement = archetypeElements[archetype];

            for (var specializationIndex = 0; specializationIndex < tiers[tierIndex].Length; specializationIndex++)
            {
                var specialization = tiers[tierIndex][specializationIndex];
                var className = $"{tierName} {specialization}";

                var statBonus = 6 * tier;
                var specializationBonus = specializationIndex switch
                {
                    0 => (str: 4, agi: 2, intel: 2, wis: 1, cha: 1, end: 3, luck: 1),
                    1 => (str: 2, agi: 4, intel: 3, wis: 2, cha: 2, end: 2, luck: 2),
                    _ => (str: 3, agi: 3, intel: 2, wis: 3, cha: 1, end: 2, luck: 3)
                };

                var stats = archetype switch
                {
                    "Warrior" => (baseStr: 20, baseAgi: 12, baseInt: 8, baseWis: 10, baseCha: 8, baseEnd: 18, baseLuck: 10),
                    "Archer" => (baseStr: 12, baseAgi: 20, baseInt: 10, baseWis: 12, baseCha: 9, baseEnd: 11, baseLuck: 12),
                    "Rogue" => (baseStr: 14, baseAgi: 19, baseInt: 9, baseWis: 10, baseCha: 11, baseEnd: 10, baseLuck: 14),
                    _ => (baseStr: 8, baseAgi: 11, baseInt: 21, baseWis: 18, baseCha: 12, baseEnd: 9, baseLuck: 10)
                };

                var tierSkillPrefix = $"{specialization} {tierName}";
                var attackSkill = new Skill
                {
                    Name = $"{tierSkillPrefix} Assault",
                    Description = $"A {tierName.ToLower()} attack technique mastered by {specialization}.",
                    Type = SkillType.Active,
                    RequiredLevel = requiredLevel,
                    Cooldown = Math.Max(1, 4 - tier),
                    ManaCost = 8 * tier,
                    StaminaCost = 6 * tier,
                    AttackPower = 16 + (8 * tier),
                    DefensePower = 4 + (2 * tier),
                    SpeedModifier = 2 + tier,
                    MagicPower = 6 + (5 * tier),
                    Element = primaryElement,
                    ElementPowerMultiplier = 1.15 + (0.08 * tier)
                };

                var defenseSkill = new Skill
                {
                    Name = $"{tierSkillPrefix} Aegis",
                    Description = $"A {tierName.ToLower()} guard that fortifies the {specialization}.",
                    Type = SkillType.Passive,
                    RequiredLevel = requiredLevel,
                    Cooldown = 0,
                    ManaCost = 3 * tier,
                    StaminaCost = 3 * tier,
                    AttackPower = 2 + tier,
                    DefensePower = 12 + (7 * tier),
                    SpeedModifier = 1 + tier,
                    MagicPower = 5 + (4 * tier),
                    Element = primaryElement,
                    ElementPowerMultiplier = 1.0 + (0.04 * tier)
                };

                var pinnacleSkill = new Skill
                {
                    Name = $"{tierSkillPrefix} Pinnacle",
                    Description = $"The signature {tierName.ToLower()} move of the {specialization}.",
                    Type = SkillType.Ultimate,
                    RequiredLevel = requiredLevel,
                    Cooldown = Math.Max(2, 5 - tier),
                    ManaCost = 14 * tier,
                    StaminaCost = 10 * tier,
                    AttackPower = 24 + (10 * tier),
                    DefensePower = 6 + (3 * tier),
                    SpeedModifier = 2 + tier,
                    MagicPower = 10 + (7 * tier),
                    Element = primaryElement,
                    ElementPowerMultiplier = 1.25 + (0.1 * tier)
                };

                var advancedClass = new RpgApi.Models.Class
                {
                    Name = className,
                    Archetype = archetype,
                    Tier = tier,
                    RequiredLevel = requiredLevel,
                    IsAdvanced = true,
                    Branch = specializationIndex,
                    BaseStrength = stats.baseStr + statBonus + specializationBonus.str,
                    BaseAgility = stats.baseAgi + statBonus + specializationBonus.agi,
                    BaseIntelligence = stats.baseInt + statBonus + specializationBonus.intel,
                    BaseWisdom = stats.baseWis + statBonus + specializationBonus.wis,
                    BaseCharisma = stats.baseCha + statBonus + specializationBonus.cha,
                    BaseEndurance = stats.baseEnd + statBonus + specializationBonus.end,
                    BaseLuck = stats.baseLuck + statBonus + specializationBonus.luck,
                    Skills = new List<Skill>
                    {
                        new StarterStrike(),
                        new StarterGuard(),
                        attackSkill,
                        defenseSkill,
                        pinnacleSkill
                    }
                };

                advancedSeeds.Add(advancedClass);
            }
        }
    }

    existingClassesByName = db.Classes
        .Include(c => c.Skills)
        .ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);

    foreach (var seed in advancedSeeds)
    {
        if (!existingClassesByName.TryGetValue(seed.Name, out var existingAdvancedClass))
        {
            db.Classes.Add(seed);
            continue;
        }

        existingAdvancedClass.Archetype = seed.Archetype;
        existingAdvancedClass.Tier = seed.Tier;
        existingAdvancedClass.RequiredLevel = seed.RequiredLevel;
        existingAdvancedClass.IsAdvanced = seed.IsAdvanced;
        existingAdvancedClass.Branch = seed.Branch;

        MergeMissingSkills(existingAdvancedClass, seed.Skills);
    }

    db.SaveChanges();

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
        new PhoenixGuardian(),
        new OlympianSkyfather(),
        new OlympianSeaLord(),
        new OlympianUnderworldKing(),
        new OlympianWarbringer(),
        new OlympianRadiantOracle()
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
        ("Goblin Captain", "Goblin", 10, EnemyType.Common),
        ("Goblin Hexer", "Goblin", 14, EnemyType.Elite),
        ("Orc Berserker", "Orc", 18, EnemyType.Elite),
        ("Orc Warchief", "Orc", 24, EnemyType.Elite),
        ("Skeleton Knight", "Skeleton", 12, EnemyType.Common),
        ("Skeleton Reaver", "Skeleton", 20, EnemyType.Elite),
        ("Dark Mage Disciple", "Dark Mage", 16, EnemyType.Elite),
        ("Dark Mage Hierophant", "Dark Mage", 28, EnemyType.Boss),
        ("Troll Ravager", "Troll", 22, EnemyType.Elite),
        ("Troll Juggernaut", "Troll", 35, EnemyType.Boss),
        ("Shadow Duelist", "Shadow Assassin", 26, EnemyType.Elite),
        ("Shadow Executioner", "Shadow Assassin", 40, EnemyType.Boss),
        ("Phoenix Warden", "Phoenix Guardian", 45, EnemyType.Boss),
        ("Lich Disciple", "Lich", 32, EnemyType.Boss),
        ("Lich Archon", "Lich", 60, EnemyType.Legendary),
        ("Ancient Dragon Wyrmlord", "Dragon", 55, EnemyType.Boss),
        ("Ancient Dragon Tyrant", "Dragon", 80, EnemyType.Legendary),
        ("Ancient Dragon Worldburner", "Dragon", 100, EnemyType.Legendary),
        ("Lich Overlord", "Lich", 100, EnemyType.Boss),
        ("Phoenix Guardian", "Phoenix Guardian", 100, EnemyType.Legendary),
        ("Ancient Red Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Ancient Blue Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Ancient Green Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Ancient Black Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Ancient White Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Ancient Gold Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Ancient Silver Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Ancient Bronze Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Ancient Copper Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Legendary Brass Dragon", "Dragon", 100, EnemyType.Legendary),
        ("Godlike Phoenix Prime", "Phoenix Guardian", 100, EnemyType.Godlike),
        ("Godlike Lich Ascendant", "Lich", 200, EnemyType.Godlike),
        ("Godlike Arcane Dragon", "Dragon", 300, EnemyType.Godlike),
        ("Godlike Shadow Regent", "Shadow Assassin", 400, EnemyType.Godlike),
        ("Godlike Abyss Warden", "Dark Mage", 500, EnemyType.Godlike),
        ("Godlike Inferno Tyrant", "Dragon", 600, EnemyType.Godlike),
        ("Godlike Soul Devourer", "Lich", 700, EnemyType.Godlike),
        ("Godlike Void Sovereign", "Shadow Assassin", 800, EnemyType.Godlike),
        ("Godlike Celestial Harbinger", "Phoenix Guardian", 900, EnemyType.Godlike),
        ("Godlike Eternal Omnarch", "Dragon", 1000, EnemyType.Godlike),
        ("Zeus, Skyfather of Storms", "Olympian Skyfather", 1100, EnemyType.Godlike),
        ("Poseidon, Tidelord of Ruin", "Olympian Sea Lord", 1200, EnemyType.Godlike),
        ("Hades, Sovereign of the Underworld", "Olympian Underworld King", 1300, EnemyType.Godlike),
        ("Athena, Aegis Strategos", "Olympian Skyfather", 1400, EnemyType.Godlike),
        ("Ares, War Incarnate", "Olympian Warbringer", 1500, EnemyType.Godlike),
        ("Artemis, Moonhunt Eternal", "Olympian Sea Lord", 1600, EnemyType.Godlike),
        ("Apollo, Solar Oracle", "Olympian Radiant Oracle", 1700, EnemyType.Godlike),
        ("Hephaestus, Forge of Cataclysm", "Olympian Warbringer", 1800, EnemyType.Godlike),
        ("Hermes, Swift Beyond Dawn", "Olympian Skyfather", 1900, EnemyType.Godlike),
        ("Hera, Throne of Olympus", "Olympian Radiant Oracle", 2000, EnemyType.Godlike)
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

    // Keep Greek god enemies aligned with the dedicated Olympian enemy classes.
    var greekGodClassMap = new Dictionary<string, (string EnemyClassName, int Level)>(StringComparer.OrdinalIgnoreCase)
    {
        ["Zeus, Skyfather of Storms"] = ("Olympian Skyfather", 1100),
        ["Poseidon, Tidelord of Ruin"] = ("Olympian Sea Lord", 1200),
        ["Hades, Sovereign of the Underworld"] = ("Olympian Underworld King", 1300),
        ["Athena, Aegis Strategos"] = ("Olympian Skyfather", 1400),
        ["Ares, War Incarnate"] = ("Olympian Warbringer", 1500),
        ["Artemis, Moonhunt Eternal"] = ("Olympian Sea Lord", 1600),
        ["Apollo, Solar Oracle"] = ("Olympian Radiant Oracle", 1700),
        ["Hephaestus, Forge of Cataclysm"] = ("Olympian Warbringer", 1800),
        ["Hermes, Swift Beyond Dawn"] = ("Olympian Skyfather", 1900),
        ["Hera, Throne of Olympus"] = ("Olympian Radiant Oracle", 2000)
    };

    var greekGods = db.Enemies
        .Where(e => greekGodClassMap.Keys.Contains(e.Name))
        .ToList();

    foreach (var greekGod in greekGods)
    {
        var (enemyClassName, level) = greekGodClassMap[greekGod.Name];
        if (!enemyClassMap.TryGetValue(enemyClassName, out var greekClass))
        {
            continue;
        }

        greekGod.EnemyClassId = greekClass.Id;
        greekGod.Type = EnemyType.Godlike;
        greekGod.Level = level;
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
