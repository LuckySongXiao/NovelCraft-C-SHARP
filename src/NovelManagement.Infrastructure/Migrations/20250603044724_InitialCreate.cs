using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NovelManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CoverImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Settings = table.Column<string>(type: "TEXT", nullable: true),
                    Statistics = table.Column<string>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProjectPath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CultivationSystems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CultivationMethod = table.Column<string>(type: "TEXT", nullable: true),
                    RealmDivision = table.Column<string>(type: "TEXT", nullable: true),
                    BreakthroughConditions = table.Column<string>(type: "TEXT", nullable: true),
                    CultivationResources = table.Column<string>(type: "TEXT", nullable: true),
                    Characteristics = table.Column<string>(type: "TEXT", nullable: true),
                    Risks = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CultivationSystems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CultivationSystems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurrencySystems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MonetarySystem = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    BaseCurrency = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CurrencyTypes = table.Column<string>(type: "TEXT", nullable: true),
                    ExchangeRates = table.Column<string>(type: "TEXT", nullable: true),
                    IssuingAuthority = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    InflationRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    InterestRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    MoneySupply = table.Column<long>(type: "INTEGER", nullable: true),
                    ExchangeRateVolatility = table.Column<decimal>(type: "TEXT", nullable: true),
                    FinancialServices = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    HistoricalBackground = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    EstablishedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsageScope = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RegulatoryAuthority = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LegalFramework = table.Column<string>(type: "TEXT", nullable: true),
                    EconomicIndicators = table.Column<string>(type: "TEXT", nullable: true),
                    RiskAssessment = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencySystems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrencySystems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Factions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LeaderId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MemberCount = table.Column<int>(type: "INTEGER", nullable: true),
                    PowerRating = table.Column<int>(type: "INTEGER", nullable: true),
                    InfluenceRating = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    History = table.Column<string>(type: "TEXT", nullable: true),
                    SpecialAbilities = table.Column<string>(type: "TEXT", nullable: true),
                    Resources = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EmblemPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Headquarters = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Territory = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Factions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoliticalSystems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Structure = table.Column<string>(type: "TEXT", nullable: true),
                    PowerDistribution = table.Column<string>(type: "TEXT", nullable: true),
                    LegalSystem = table.Column<string>(type: "TEXT", nullable: true),
                    ElectionSystem = table.Column<string>(type: "TEXT", nullable: true),
                    AdministrativeSystem = table.Column<string>(type: "TEXT", nullable: true),
                    MilitarySystem = table.Column<string>(type: "TEXT", nullable: true),
                    EconomicSystem = table.Column<string>(type: "TEXT", nullable: true),
                    SocialHierarchy = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticalSystems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoliticalSystems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Races",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Population = table.Column<long>(type: "INTEGER", nullable: false),
                    MainTerritory = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    RulingArea = table.Column<string>(type: "TEXT", nullable: true),
                    PowerLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Influence = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Characteristics = table.Column<string>(type: "TEXT", nullable: true),
                    CulturalBackground = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    AverageLifespan = table.Column<int>(type: "INTEGER", nullable: true),
                    BirthRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    PrimaryLanguage = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    PrimaryReligion = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RacialAbilities = table.Column<string>(type: "TEXT", nullable: true),
                    RacialWeaknesses = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Races", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Races_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Volumes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    EstimatedWordCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualWordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Progress = table.Column<decimal>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Volumes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Volumes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorldSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    Rules = table.Column<string>(type: "TEXT", nullable: true),
                    History = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedSettings = table.Column<string>(type: "TEXT", nullable: true),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorldSettings_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorldSettings_WorldSettings_ParentId",
                        column: x => x.ParentId,
                        principalTable: "WorldSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CultivationLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    BreakthroughCondition = table.Column<string>(type: "TEXT", nullable: true),
                    Abilities = table.Column<string>(type: "TEXT", nullable: true),
                    CultivationTime = table.Column<string>(type: "TEXT", nullable: true),
                    CultivationSystemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CultivationLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CultivationLevels_CultivationSystems_CultivationSystemId",
                        column: x => x.CultivationSystemId,
                        principalTable: "CultivationSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurrencySystemFaction",
                columns: table => new
                {
                    UsedCurrencySystemsId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UsingFactionsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrencySystemFaction", x => new { x.UsedCurrencySystemsId, x.UsingFactionsId });
                    table.ForeignKey(
                        name: "FK_CurrencySystemFaction_CurrencySystems_UsedCurrencySystemsId",
                        column: x => x.UsedCurrencySystemsId,
                        principalTable: "CurrencySystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CurrencySystemFaction_Factions_UsingFactionsId",
                        column: x => x.UsingFactionsId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FactionRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceFactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetFactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelationshipType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Intensity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RelationshipName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DevelopmentHistory = table.Column<string>(type: "TEXT", nullable: true),
                    KeyEvents = table.Column<string>(type: "TEXT", nullable: true),
                    Impact = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsBidirectional = table.Column<bool>(type: "INTEGER", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    MilitaryComparison = table.Column<string>(type: "TEXT", nullable: true),
                    EconomicRelations = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_Factions_SourceFactionId",
                        column: x => x.SourceFactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FactionRelationships_Factions_TargetFactionId",
                        column: x => x.TargetFactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Resources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ControllingFactionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Rarity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExtractionDifficulty = table.Column<int>(type: "INTEGER", nullable: false),
                    EconomicValue = table.Column<int>(type: "INTEGER", nullable: false),
                    RegenerationSpeed = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ExtractionMethod = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentReserves = table.Column<long>(type: "INTEGER", nullable: true),
                    MaxReserves = table.Column<long>(type: "INTEGER", nullable: true),
                    AnnualOutput = table.Column<long>(type: "INTEGER", nullable: true),
                    DiscoveryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastExtractionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Resources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Resources_Factions_ControllingFactionId",
                        column: x => x.ControllingFactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Resources_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecretRealms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Location = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    DiscovererFactionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    DangerLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    CapacityLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    TimeLimit = table.Column<int>(type: "INTEGER", nullable: true),
                    RecommendedCultivation = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ExplorationConditions = table.Column<string>(type: "TEXT", nullable: true),
                    ExplorationRewards = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Strategy = table.Column<string>(type: "TEXT", nullable: true),
                    LastExplorationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    DiscoveryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OpenDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CloseDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ExplorationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    SuccessfulExplorationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DimensionInfo = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    EntranceInfo = table.Column<string>(type: "TEXT", nullable: true),
                    InternalStructure = table.Column<string>(type: "TEXT", nullable: true),
                    SpecialRules = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretRealms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecretRealms_Factions_DiscovererFactionId",
                        column: x => x.DiscovererFactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SecretRealms_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PoliticalPositions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Powers = table.Column<string>(type: "TEXT", nullable: true),
                    Responsibilities = table.Column<string>(type: "TEXT", nullable: true),
                    Requirements = table.Column<string>(type: "TEXT", nullable: true),
                    Term = table.Column<string>(type: "TEXT", nullable: true),
                    PoliticalSystemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoliticalPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoliticalPositions_PoliticalSystems_PoliticalSystemId",
                        column: x => x.PoliticalSystemId,
                        principalTable: "PoliticalSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Gender = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Age = table.Column<int>(type: "INTEGER", nullable: true),
                    CultivationLevel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FactionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RaceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Appearance = table.Column<string>(type: "TEXT", nullable: true),
                    Personality = table.Column<string>(type: "TEXT", nullable: true),
                    Background = table.Column<string>(type: "TEXT", nullable: true),
                    Abilities = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AvatarPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FirstAppearanceChapterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    LastAppearanceChapterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Characters_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Characters_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Characters_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RaceRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceRaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetRaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelationshipType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Strength = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    History = table.Column<string>(type: "TEXT", nullable: true),
                    KeyEvents = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    EstablishedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMutual = table.Column<bool>(type: "INTEGER", nullable: false),
                    RaceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RaceId1 = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaceRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaceRelationships_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaceRelationships_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RaceRelationships_Races_RaceId1",
                        column: x => x.RaceId1,
                        principalTable: "Races",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RaceRelationships_Races_SourceRaceId",
                        column: x => x.SourceRaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaceRelationships_Races_TargetRaceId",
                        column: x => x.TargetRaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Chapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    VolumeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ReadingTime = table.Column<int>(type: "INTEGER", nullable: true),
                    DifficultyLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    Importance = table.Column<int>(type: "INTEGER", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastEditedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    VersionNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Chapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Chapters_Volumes_VolumeId",
                        column: x => x.VolumeId,
                        principalTable: "Volumes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ResourceSecretRealm",
                columns: table => new
                {
                    RelatedResourcesId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SecretRealmId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceSecretRealm", x => new { x.RelatedResourcesId, x.SecretRealmId });
                    table.ForeignKey(
                        name: "FK_ResourceSecretRealm_Resources_RelatedResourcesId",
                        column: x => x.RelatedResourcesId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceSecretRealm_SecretRealms_SecretRealmId",
                        column: x => x.SecretRealmId,
                        principalTable: "SecretRealms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RelationshipNetworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CentralCharacterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Complexity = table.Column<int>(type: "INTEGER", nullable: false),
                    Stability = table.Column<int>(type: "INTEGER", nullable: false),
                    Influence = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    KeyEvents = table.Column<string>(type: "TEXT", nullable: true),
                    DevelopmentHistory = table.Column<string>(type: "TEXT", nullable: true),
                    NetworkRules = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    EstablishedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsPublic = table.Column<bool>(type: "INTEGER", nullable: false),
                    HierarchyLevel = table.Column<int>(type: "INTEGER", nullable: true),
                    MemberCount = table.Column<int>(type: "INTEGER", nullable: false),
                    RelationshipCount = table.Column<int>(type: "INTEGER", nullable: false),
                    NetworkDensity = table.Column<decimal>(type: "TEXT", nullable: true),
                    NetworkGraphData = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelationshipNetworks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RelationshipNetworks_Characters_CentralCharacterId",
                        column: x => x.CentralCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RelationshipNetworks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Progress = table.Column<decimal>(type: "TEXT", nullable: false),
                    StartChapterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    EndChapterId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Outline = table.Column<string>(type: "TEXT", nullable: true),
                    ConflictElements = table.Column<string>(type: "TEXT", nullable: true),
                    ThemeElements = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    EstimatedWordCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ActualWordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plots_Chapters_EndChapterId",
                        column: x => x.EndChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Plots_Chapters_StartChapterId",
                        column: x => x.StartChapterId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Plots_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterRelationshipNetwork",
                columns: table => new
                {
                    MembersId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParticipatedNetworksId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRelationshipNetwork", x => new { x.MembersId, x.ParticipatedNetworksId });
                    table.ForeignKey(
                        name: "FK_CharacterRelationshipNetwork_Characters_MembersId",
                        column: x => x.MembersId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterRelationshipNetwork_RelationshipNetworks_ParticipatedNetworksId",
                        column: x => x.ParticipatedNetworksId,
                        principalTable: "RelationshipNetworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterRelationships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceCharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetCharacterId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelationshipType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Intensity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RelationshipName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    DevelopmentHistory = table.Column<string>(type: "TEXT", nullable: true),
                    KeyEvents = table.Column<string>(type: "TEXT", nullable: true),
                    Impact = table.Column<string>(type: "TEXT", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Tags = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsBidirectional = table.Column<bool>(type: "INTEGER", nullable: false),
                    Importance = table.Column<int>(type: "INTEGER", nullable: false),
                    RelationshipNetworkId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedBy = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterRelationships_Characters_SourceCharacterId",
                        column: x => x.SourceCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterRelationships_Characters_TargetCharacterId",
                        column: x => x.TargetCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CharacterRelationships_RelationshipNetworks_RelationshipNetworkId",
                        column: x => x.RelationshipNetworkId,
                        principalTable: "RelationshipNetworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChapterPlot",
                columns: table => new
                {
                    InvolvedChaptersId = table.Column<Guid>(type: "TEXT", nullable: false),
                    InvolvedPlotsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterPlot", x => new { x.InvolvedChaptersId, x.InvolvedPlotsId });
                    table.ForeignKey(
                        name: "FK_ChapterPlot_Chapters_InvolvedChaptersId",
                        column: x => x.InvolvedChaptersId,
                        principalTable: "Chapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChapterPlot_Plots_InvolvedPlotsId",
                        column: x => x.InvolvedPlotsId,
                        principalTable: "Plots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterPlot",
                columns: table => new
                {
                    RelatedCharactersId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelatedPlotsId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterPlot", x => new { x.RelatedCharactersId, x.RelatedPlotsId });
                    table.ForeignKey(
                        name: "FK_CharacterPlot_Characters_RelatedCharactersId",
                        column: x => x.RelatedCharactersId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterPlot_Plots_RelatedPlotsId",
                        column: x => x.RelatedPlotsId,
                        principalTable: "Plots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterPlot_InvolvedPlotsId",
                table: "ChapterPlot",
                column: "InvolvedPlotsId");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_OrderIndex",
                table: "Chapters",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_Status",
                table: "Chapters",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Chapters_VolumeId",
                table: "Chapters",
                column: "VolumeId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterPlot_RelatedPlotsId",
                table: "CharacterPlot",
                column: "RelatedPlotsId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationshipNetwork_ParticipatedNetworksId",
                table: "CharacterRelationshipNetwork",
                column: "ParticipatedNetworksId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationships_RelationshipNetworkId",
                table: "CharacterRelationships",
                column: "RelationshipNetworkId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationships_RelationshipType",
                table: "CharacterRelationships",
                column: "RelationshipType");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationships_SourceCharacterId",
                table: "CharacterRelationships",
                column: "SourceCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterRelationships_TargetCharacterId",
                table: "CharacterRelationships",
                column: "TargetCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_FactionId",
                table: "Characters",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Importance",
                table: "Characters",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_ProjectId",
                table: "Characters",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_RaceId",
                table: "Characters",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Characters_Type",
                table: "Characters",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationLevels_CultivationSystemId",
                table: "CultivationLevels",
                column: "CultivationSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationLevels_OrderIndex",
                table: "CultivationLevels",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationSystems_Importance",
                table: "CultivationSystems",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationSystems_OrderIndex",
                table: "CultivationSystems",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationSystems_ProjectId",
                table: "CultivationSystems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CultivationSystems_Type",
                table: "CultivationSystems",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencySystemFaction_UsingFactionsId",
                table: "CurrencySystemFaction",
                column: "UsingFactionsId");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencySystems_Importance",
                table: "CurrencySystems",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencySystems_IsActive",
                table: "CurrencySystems",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencySystems_MonetarySystem",
                table: "CurrencySystems",
                column: "MonetarySystem");

            migrationBuilder.CreateIndex(
                name: "IX_CurrencySystems_ProjectId",
                table: "CurrencySystems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_RelationshipType",
                table: "FactionRelationships",
                column: "RelationshipType");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_SourceFactionId",
                table: "FactionRelationships",
                column: "SourceFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelationships_TargetFactionId",
                table: "FactionRelationships",
                column: "TargetFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Factions_InfluenceRating",
                table: "Factions",
                column: "InfluenceRating");

            migrationBuilder.CreateIndex(
                name: "IX_Factions_PowerRating",
                table: "Factions",
                column: "PowerRating");

            migrationBuilder.CreateIndex(
                name: "IX_Factions_ProjectId",
                table: "Factions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Factions_Type",
                table: "Factions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Plots_EndChapterId",
                table: "Plots",
                column: "EndChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Plots_Importance",
                table: "Plots",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_Plots_Priority",
                table: "Plots",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Plots_ProjectId",
                table: "Plots",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Plots_StartChapterId",
                table: "Plots",
                column: "StartChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_Plots_Status",
                table: "Plots",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Plots_Type",
                table: "Plots",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticalPositions_Level",
                table: "PoliticalPositions",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticalPositions_PoliticalSystemId",
                table: "PoliticalPositions",
                column: "PoliticalSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticalSystems_Importance",
                table: "PoliticalSystems",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticalSystems_OrderIndex",
                table: "PoliticalSystems",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticalSystems_ProjectId",
                table: "PoliticalSystems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PoliticalSystems_Type",
                table: "PoliticalSystems",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_LastAccessedAt",
                table: "Projects",
                column: "LastAccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Name",
                table: "Projects",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Status",
                table: "Projects",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_Type",
                table: "Projects",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRelationships_Importance",
                table: "RaceRelationships",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRelationships_ProjectId",
                table: "RaceRelationships",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRelationships_RaceId",
                table: "RaceRelationships",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRelationships_RaceId1",
                table: "RaceRelationships",
                column: "RaceId1");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRelationships_RelationshipType",
                table: "RaceRelationships",
                column: "RelationshipType");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRelationships_SourceRaceId",
                table: "RaceRelationships",
                column: "SourceRaceId");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRelationships_Status",
                table: "RaceRelationships",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRelationships_Strength",
                table: "RaceRelationships",
                column: "Strength");

            migrationBuilder.CreateIndex(
                name: "IX_RaceRelationships_TargetRaceId",
                table: "RaceRelationships",
                column: "TargetRaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Races_Importance",
                table: "Races",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_Races_Influence",
                table: "Races",
                column: "Influence");

            migrationBuilder.CreateIndex(
                name: "IX_Races_PowerLevel",
                table: "Races",
                column: "PowerLevel");

            migrationBuilder.CreateIndex(
                name: "IX_Races_ProjectId",
                table: "Races",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Races_Status",
                table: "Races",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Races_Type",
                table: "Races",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipNetworks_CentralCharacterId",
                table: "RelationshipNetworks",
                column: "CentralCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipNetworks_Complexity",
                table: "RelationshipNetworks",
                column: "Complexity");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipNetworks_Importance",
                table: "RelationshipNetworks",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipNetworks_Influence",
                table: "RelationshipNetworks",
                column: "Influence");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipNetworks_ProjectId",
                table: "RelationshipNetworks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipNetworks_Status",
                table: "RelationshipNetworks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RelationshipNetworks_Type",
                table: "RelationshipNetworks",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ControllingFactionId",
                table: "Resources",
                column: "ControllingFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_EconomicValue",
                table: "Resources",
                column: "EconomicValue");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Importance",
                table: "Resources",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_ProjectId",
                table: "Resources",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Rarity",
                table: "Resources",
                column: "Rarity");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Status",
                table: "Resources",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Type",
                table: "Resources",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceSecretRealm_SecretRealmId",
                table: "ResourceSecretRealm",
                column: "SecretRealmId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretRealms_DangerLevel",
                table: "SecretRealms",
                column: "DangerLevel");

            migrationBuilder.CreateIndex(
                name: "IX_SecretRealms_DiscovererFactionId",
                table: "SecretRealms",
                column: "DiscovererFactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretRealms_Importance",
                table: "SecretRealms",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_SecretRealms_ProjectId",
                table: "SecretRealms",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretRealms_Status",
                table: "SecretRealms",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SecretRealms_Type",
                table: "SecretRealms",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Volumes_OrderIndex",
                table: "Volumes",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_Volumes_ProjectId",
                table: "Volumes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Volumes_Status",
                table: "Volumes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_WorldSettings_Category",
                table: "WorldSettings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WorldSettings_Importance",
                table: "WorldSettings",
                column: "Importance");

            migrationBuilder.CreateIndex(
                name: "IX_WorldSettings_OrderIndex",
                table: "WorldSettings",
                column: "OrderIndex");

            migrationBuilder.CreateIndex(
                name: "IX_WorldSettings_ParentId",
                table: "WorldSettings",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_WorldSettings_ProjectId",
                table: "WorldSettings",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_WorldSettings_Type",
                table: "WorldSettings",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChapterPlot");

            migrationBuilder.DropTable(
                name: "CharacterPlot");

            migrationBuilder.DropTable(
                name: "CharacterRelationshipNetwork");

            migrationBuilder.DropTable(
                name: "CharacterRelationships");

            migrationBuilder.DropTable(
                name: "CultivationLevels");

            migrationBuilder.DropTable(
                name: "CurrencySystemFaction");

            migrationBuilder.DropTable(
                name: "FactionRelationships");

            migrationBuilder.DropTable(
                name: "PoliticalPositions");

            migrationBuilder.DropTable(
                name: "RaceRelationships");

            migrationBuilder.DropTable(
                name: "ResourceSecretRealm");

            migrationBuilder.DropTable(
                name: "WorldSettings");

            migrationBuilder.DropTable(
                name: "Plots");

            migrationBuilder.DropTable(
                name: "RelationshipNetworks");

            migrationBuilder.DropTable(
                name: "CultivationSystems");

            migrationBuilder.DropTable(
                name: "CurrencySystems");

            migrationBuilder.DropTable(
                name: "PoliticalSystems");

            migrationBuilder.DropTable(
                name: "Resources");

            migrationBuilder.DropTable(
                name: "SecretRealms");

            migrationBuilder.DropTable(
                name: "Chapters");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "Volumes");

            migrationBuilder.DropTable(
                name: "Factions");

            migrationBuilder.DropTable(
                name: "Races");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
