using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RpgApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRegenColumnsToCharacterAndEnemy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Classes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    BaseStrength = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseAgility = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseIntelligence = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseWisdom = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseCharisma = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseEndurance = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseLuck = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Classes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnemyClasses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseStrength = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseAgility = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseIntelligence = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseWisdom = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseCharisma = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseEndurance = table.Column<int>(type: "INTEGER", nullable: false),
                    BaseLuck = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnemyClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FightSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterName = table.Column<string>(type: "TEXT", nullable: false),
                    EnemyName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActionAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    LockedByPlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterCurrentHp = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyCurrentHp = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterMaxHp = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyMaxHp = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterCurrentMana = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterMaxMana = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyCurrentMana = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyMaxMana = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterCurrentStamina = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterMaxStamina = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyCurrentStamina = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyMaxStamina = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterHealthRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterManaRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterStaminaRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyHealthRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyManaRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyStaminaRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterAttack = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterDefense = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyAttack = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyDefense = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    EnemyLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    IsVictory = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentTurn = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterCooldownJson = table.Column<string>(type: "TEXT", nullable: false),
                    EnemyCooldownJson = table.Column<string>(type: "TEXT", nullable: false),
                    CharacterSkillIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                    EnemySkillIdsJson = table.Column<string>(type: "TEXT", nullable: false),
                    CharacterActivePassiveSkillsJson = table.Column<string>(type: "TEXT", nullable: false),
                    EnemyActivePassiveSkillsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FightSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Enemies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxHealth = table.Column<int>(type: "INTEGER", nullable: false),
                    Mana = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxMana = table.Column<int>(type: "INTEGER", nullable: false),
                    Stamina = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxStamina = table.Column<int>(type: "INTEGER", nullable: false),
                    HealthRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    ManaRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    StaminaRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Defense = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<int>(type: "INTEGER", nullable: false),
                    Magic = table.Column<int>(type: "INTEGER", nullable: false),
                    ExperienceReward = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EnemyClassId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enemies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enemies_EnemyClasses_EnemyClassId",
                        column: x => x.EnemyClassId,
                        principalTable: "EnemyClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FightMoves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FightSessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Turn = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPlayer = table.Column<bool>(type: "INTEGER", nullable: false),
                    SkillId = table.Column<int>(type: "INTEGER", nullable: false),
                    Damage = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FightMoves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FightMoves_FightSessions_FightSessionId",
                        column: x => x.FightSessionId,
                        principalTable: "FightSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Health = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxHealth = table.Column<int>(type: "INTEGER", nullable: false),
                    Mana = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxMana = table.Column<int>(type: "INTEGER", nullable: false),
                    Stamina = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxStamina = table.Column<int>(type: "INTEGER", nullable: false),
                    HealthRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    ManaRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    StaminaRegen = table.Column<int>(type: "INTEGER", nullable: false),
                    Attack = table.Column<int>(type: "INTEGER", nullable: false),
                    Defense = table.Column<int>(type: "INTEGER", nullable: false),
                    Speed = table.Column<int>(type: "INTEGER", nullable: false),
                    Magic = table.Column<int>(type: "INTEGER", nullable: false),
                    Experience = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PlayerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ClassId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Characters_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    ManaCost = table.Column<int>(type: "INTEGER", nullable: false),
                    StaminaCost = table.Column<int>(type: "INTEGER", nullable: false),
                    Cooldown = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredStrength = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredAgility = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredIntelligence = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredWisdom = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredCharisma = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredEndurance = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredLuck = table.Column<int>(type: "INTEGER", nullable: false),
                    StrengthModifier = table.Column<int>(type: "INTEGER", nullable: false),
                    AgilityModifier = table.Column<int>(type: "INTEGER", nullable: false),
                    IntelligenceModifier = table.Column<int>(type: "INTEGER", nullable: false),
                    WisdomModifier = table.Column<int>(type: "INTEGER", nullable: false),
                    CharismaModifier = table.Column<int>(type: "INTEGER", nullable: false),
                    EnduranceModifier = table.Column<int>(type: "INTEGER", nullable: false),
                    LuckModifier = table.Column<int>(type: "INTEGER", nullable: false),
                    AttackPower = table.Column<int>(type: "INTEGER", nullable: false),
                    DefensePower = table.Column<int>(type: "INTEGER", nullable: false),
                    SpeedModifier = table.Column<int>(type: "INTEGER", nullable: false),
                    MagicPower = table.Column<int>(type: "INTEGER", nullable: false),
                    CharacterId = table.Column<int>(type: "INTEGER", nullable: true),
                    EnemyId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skills_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Skills_Enemies_EnemyId",
                        column: x => x.EnemyId,
                        principalTable: "Enemies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ClassSkill",
                columns: table => new
                {
                    ClassesId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassSkill", x => new { x.ClassesId, x.SkillsId });
                    table.ForeignKey(
                        name: "FK_ClassSkill_Classes_ClassesId",
                        column: x => x.ClassesId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClassSkill_Skills_SkillsId",
                        column: x => x.SkillsId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EnemyClassSkill",
                columns: table => new
                {
                    EnemyClassesId = table.Column<int>(type: "INTEGER", nullable: false),
                    SkillsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnemyClassSkill", x => new { x.EnemyClassesId, x.SkillsId });
                    table.ForeignKey(
                        name: "FK_EnemyClassSkill_EnemyClasses_EnemyClassesId",
                        column: x => x.EnemyClassesId,
                        principalTable: "EnemyClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnemyClassSkill_Skills_SkillsId",
                        column: x => x.SkillsId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ClassId",
                table: "Characters",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_PlayerId",
                table: "Characters",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ClassSkill_SkillsId",
                table: "ClassSkill",
                column: "SkillsId");

            migrationBuilder.CreateIndex(
                name: "IX_Enemies_EnemyClassId",
                table: "Enemies",
                column: "EnemyClassId");

            migrationBuilder.CreateIndex(
                name: "IX_EnemyClassSkill_SkillsId",
                table: "EnemyClassSkill",
                column: "SkillsId");

            migrationBuilder.CreateIndex(
                name: "IX_FightMoves_FightSessionId",
                table: "FightMoves",
                column: "FightSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_FightSessions_Status",
                table: "FightSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Email",
                table: "Players",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Players_Username",
                table: "Players",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_CharacterId",
                table: "Skills",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_EnemyId",
                table: "Skills",
                column: "EnemyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassSkill");

            migrationBuilder.DropTable(
                name: "EnemyClassSkill");

            migrationBuilder.DropTable(
                name: "FightMoves");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "FightSessions");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Enemies");

            migrationBuilder.DropTable(
                name: "Classes");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "EnemyClasses");
        }
    }
}
