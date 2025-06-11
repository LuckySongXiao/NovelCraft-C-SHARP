using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;

namespace NovelManagement.Infrastructure.Data;

/// <summary>
/// 小说管理系统数据库上下文
/// </summary>
public class NovelManagementDbContext : DbContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options">数据库上下文选项</param>
    public NovelManagementDbContext(DbContextOptions<NovelManagementDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// 项目表
    /// </summary>
    public DbSet<Project> Projects { get; set; } = null!;

    /// <summary>
    /// 卷宗表
    /// </summary>
    public DbSet<Volume> Volumes { get; set; } = null!;

    /// <summary>
    /// 章节表
    /// </summary>
    public DbSet<Chapter> Chapters { get; set; } = null!;

    /// <summary>
    /// 角色表
    /// </summary>
    public DbSet<Character> Characters { get; set; } = null!;

    /// <summary>
    /// 势力表
    /// </summary>
    public DbSet<Faction> Factions { get; set; } = null!;

    /// <summary>
    /// 角色关系表
    /// </summary>
    public DbSet<CharacterRelationship> CharacterRelationships { get; set; } = null!;

    /// <summary>
    /// 势力关系表
    /// </summary>
    public DbSet<FactionRelationship> FactionRelationships { get; set; } = null!;

    /// <summary>
    /// 世界设定表
    /// </summary>
    public DbSet<WorldSetting> WorldSettings { get; set; } = null!;

    /// <summary>
    /// 修炼体系表
    /// </summary>
    public DbSet<CultivationSystem> CultivationSystems { get; set; } = null!;

    /// <summary>
    /// 修炼等级表
    /// </summary>
    public DbSet<CultivationLevel> CultivationLevels { get; set; } = null!;

    /// <summary>
    /// 政治体系表
    /// </summary>
    public DbSet<PoliticalSystem> PoliticalSystems { get; set; } = null!;

    /// <summary>
    /// 政治职位表
    /// </summary>
    public DbSet<PoliticalPosition> PoliticalPositions { get; set; } = null!;

    /// <summary>
    /// 剧情表
    /// </summary>
    public DbSet<Plot> Plots { get; set; } = null!;

    /// <summary>
    /// 资源表
    /// </summary>
    public DbSet<Resource> Resources { get; set; } = null!;

    /// <summary>
    /// 种族表
    /// </summary>
    public DbSet<Race> Races { get; set; } = null!;

    /// <summary>
    /// 种族关系表
    /// </summary>
    public DbSet<RaceRelationship> RaceRelationships { get; set; } = null!;

    /// <summary>
    /// 秘境表
    /// </summary>
    public DbSet<SecretRealm> SecretRealms { get; set; } = null!;

    /// <summary>
    /// 货币体系表
    /// </summary>
    public DbSet<CurrencySystem> CurrencySystems { get; set; } = null!;

    /// <summary>
    /// 关系网络表
    /// </summary>
    public DbSet<RelationshipNetwork> RelationshipNetworks { get; set; } = null!;

    /// <summary>
    /// 配置模型
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置项目实体
        ConfigureProject(modelBuilder);

        // 配置卷宗实体
        ConfigureVolume(modelBuilder);

        // 配置章节实体
        ConfigureChapter(modelBuilder);

        // 配置角色实体
        ConfigureCharacter(modelBuilder);

        // 配置势力实体
        ConfigureFaction(modelBuilder);

        // 配置角色关系实体
        ConfigureCharacterRelationship(modelBuilder);

        // 配置势力关系实体
        ConfigureFactionRelationship(modelBuilder);

        // 配置世界设定实体
        ConfigureWorldSetting(modelBuilder);

        // 配置修炼体系实体
        ConfigureCultivationSystem(modelBuilder);

        // 配置修炼等级实体
        ConfigureCultivationLevel(modelBuilder);

        // 配置政治体系实体
        ConfigurePoliticalSystem(modelBuilder);

        // 配置政治职位实体
        ConfigurePoliticalPosition(modelBuilder);

        // 配置剧情实体
        ConfigurePlot(modelBuilder);

        // 配置资源实体
        ConfigureResource(modelBuilder);

        // 配置种族实体
        ConfigureRace(modelBuilder);

        // 配置种族关系实体
        ConfigureRaceRelationship(modelBuilder);

        // 配置秘境实体
        ConfigureSecretRealm(modelBuilder);

        // 配置货币体系实体
        ConfigureCurrencySystem(modelBuilder);

        // 配置关系网络实体
        ConfigureRelationshipNetwork(modelBuilder);

        // 配置软删除全局过滤器
        ConfigureSoftDelete(modelBuilder);
    }

    /// <summary>
    /// 配置项目实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureProject(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CoverImagePath).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.ProjectPath).HasMaxLength(1000);

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.LastAccessedAt);
        });
    }

    /// <summary>
    /// 配置卷宗实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureVolume(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Volume>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Volumes)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Order);
        });
    }

    /// <summary>
    /// 配置章节实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureChapter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Type).HasMaxLength(50);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.Volume)
                  .WithMany(v => v.Chapters)
                  .HasForeignKey(e => e.VolumeId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.VolumeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Order);
        });
    }

    /// <summary>
    /// 配置角色实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureCharacter(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.CultivationLevel).HasMaxLength(100);
            entity.Property(e => e.AvatarPath).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Characters)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Faction)
                  .WithMany(f => f.Members)
                  .HasForeignKey(e => e.FactionId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Race)
                  .WithMany(r => r.Members)
                  .HasForeignKey(e => e.RaceId)
                  .OnDelete(DeleteBehavior.SetNull);

            // 配置章节外键关系（可选）
            entity.Property(e => e.FirstAppearanceChapterId)
                  .IsRequired(false);

            entity.Property(e => e.LastAppearanceChapterId)
                  .IsRequired(false);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.FactionId);
            entity.HasIndex(e => e.RaceId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Importance);
            entity.HasIndex(e => e.FirstAppearanceChapterId);
            entity.HasIndex(e => e.LastAppearanceChapterId);
        });
    }

    /// <summary>
    /// 配置势力实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureFaction(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Faction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Level).HasMaxLength(50);
            entity.Property(e => e.EmblemPath).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Headquarters).HasMaxLength(200);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Factions)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.PowerRating);
            entity.HasIndex(e => e.InfluenceRating);
        });
    }

    /// <summary>
    /// 配置角色关系实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureCharacterRelationship(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CharacterRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RelationshipType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RelationshipName).HasMaxLength(100);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.SourceCharacter)
                  .WithMany(c => c.RelationshipsAsSource)
                  .HasForeignKey(e => e.SourceCharacterId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetCharacter)
                  .WithMany(c => c.RelationshipsAsTarget)
                  .HasForeignKey(e => e.TargetCharacterId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.SourceCharacterId);
            entity.HasIndex(e => e.TargetCharacterId);
            entity.HasIndex(e => e.RelationshipType);
        });
    }

    /// <summary>
    /// 配置势力关系实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureFactionRelationship(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FactionRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RelationshipType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RelationshipName).HasMaxLength(100);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.SourceFaction)
                  .WithMany(f => f.AllianceRelationshipsAsSource)
                  .HasForeignKey(e => e.SourceFactionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetFaction)
                  .WithMany(f => f.AllianceRelationshipsAsTarget)
                  .HasForeignKey(e => e.TargetFactionId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.SourceFactionId);
            entity.HasIndex(e => e.TargetFactionId);
            entity.HasIndex(e => e.RelationshipType);
        });
    }

    /// <summary>
    /// 配置世界设定实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureWorldSetting(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorldSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.Version).HasMaxLength(20);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.WorldSettings)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Parent)
                  .WithMany(ws => ws.Children)
                  .HasForeignKey(e => e.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.ParentId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.Importance);
            entity.HasIndex(e => e.Order);
        });
    }

    /// <summary>
    /// 配置修炼体系实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureCultivationSystem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CultivationSystem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.CultivationSystems)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Importance);
            entity.HasIndex(e => e.Order);
        });
    }

    /// <summary>
    /// 配置修炼等级实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureCultivationLevel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CultivationLevel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.CultivationSystem)
                  .WithMany(cs => cs.Levels)
                  .HasForeignKey(e => e.CultivationSystemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.CultivationSystemId);
            entity.HasIndex(e => e.Order);
        });
    }

    /// <summary>
    /// 配置政治体系实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigurePoliticalSystem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PoliticalSystem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ImagePath).HasMaxLength(500);
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(50);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.PoliticalSystems)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Importance);
            entity.HasIndex(e => e.Order);
        });
    }

    /// <summary>
    /// 配置政治职位实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigurePoliticalPosition(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PoliticalPosition>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.PoliticalSystem)
                  .WithMany(ps => ps.Positions)
                  .HasForeignKey(e => e.PoliticalSystemId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.PoliticalSystemId);
            entity.HasIndex(e => e.Level);
        });
    }

    /// <summary>
    /// 配置剧情实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigurePlot(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Plot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Priority).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Plots)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.StartChapter)
                  .WithMany(c => c.PlotsAsStart)
                  .HasForeignKey(e => e.StartChapterId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.EndChapter)
                  .WithMany(c => c.PlotsAsEnd)
                  .HasForeignKey(e => e.EndChapterId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.RelatedCharacters)
                  .WithMany(c => c.RelatedPlots);

            entity.HasMany(e => e.InvolvedChapters)
                  .WithMany(c => c.InvolvedPlots);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.Importance);
        });
    }

    /// <summary>
    /// 配置资源实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureResource(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.Rarity).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RegenerationSpeed).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Resources)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ControllingFaction)
                  .WithMany(f => f.ControlledResources)
                  .HasForeignKey(e => e.ControllingFactionId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Rarity);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.EconomicValue);
            entity.HasIndex(e => e.Importance);
        });
    }

    /// <summary>
    /// 配置种族实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureRace(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Race>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MainTerritory).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PrimaryLanguage).HasMaxLength(100);
            entity.Property(e => e.PrimaryReligion).HasMaxLength(100);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Races)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Members)
                  .WithOne(c => c.Race)
                  .HasForeignKey(c => c.RaceId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PowerLevel);
            entity.HasIndex(e => e.Influence);
            entity.HasIndex(e => e.Importance);
        });
    }

    /// <summary>
    /// 配置种族关系实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureRaceRelationship(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RaceRelationship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RelationshipType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.RaceRelationships)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SourceRace)
                  .WithMany(r => r.AllyRelationshipsAsSource)
                  .HasForeignKey(e => e.SourceRaceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetRace)
                  .WithMany(r => r.AllyRelationshipsAsTarget)
                  .HasForeignKey(e => e.TargetRaceId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.SourceRaceId);
            entity.HasIndex(e => e.TargetRaceId);
            entity.HasIndex(e => e.RelationshipType);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Strength);
            entity.HasIndex(e => e.Importance);
        });
    }

    /// <summary>
    /// 配置秘境实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureSecretRealm(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SecretRealm>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RecommendedCultivation).HasMaxLength(100);
            entity.Property(e => e.DimensionInfo).HasMaxLength(200);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.SecretRealms)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.DiscovererFaction)
                  .WithMany(f => f.DiscoveredSecretRealms)
                  .HasForeignKey(e => e.DiscovererFactionId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.RelatedResources)
                  .WithMany();

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DangerLevel);
            entity.HasIndex(e => e.Importance);
        });
    }

    /// <summary>
    /// 配置货币体系实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureCurrencySystem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CurrencySystem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.MonetarySystem).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BaseCurrency).HasMaxLength(100);
            entity.Property(e => e.IssuingAuthority).HasMaxLength(200);
            entity.Property(e => e.UsageScope).HasMaxLength(500);
            entity.Property(e => e.RegulatoryAuthority).HasMaxLength(200);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.CurrencySystems)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.UsingFactions)
                  .WithMany(f => f.UsedCurrencySystems);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.MonetarySystem);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Importance);
        });
    }

    /// <summary>
    /// 配置关系网络实体
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureRelationshipNetwork(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RelationshipNetwork>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Tags).HasMaxLength(500);

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.RelationshipNetworks)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CentralCharacter)
                  .WithMany(c => c.CentralNetworks)
                  .HasForeignKey(e => e.CentralCharacterId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Members)
                  .WithMany(c => c.ParticipatedNetworks);

            entity.HasMany(e => e.Relationships)
                  .WithOne(r => r.RelationshipNetwork)
                  .HasForeignKey(r => r.RelationshipNetworkId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.ProjectId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CentralCharacterId);
            entity.HasIndex(e => e.Complexity);
            entity.HasIndex(e => e.Influence);
            entity.HasIndex(e => e.Importance);
        });
    }

    /// <summary>
    /// 配置软删除全局过滤器
    /// </summary>
    /// <param name="modelBuilder">模型构建器</param>
    private static void ConfigureSoftDelete(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Volume>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Chapter>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Character>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Faction>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CharacterRelationship>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FactionRelationship>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorldSetting>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CultivationSystem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CultivationLevel>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PoliticalSystem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PoliticalPosition>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Plot>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Resource>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Race>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RaceRelationship>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SecretRealm>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CurrencySystem>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RelationshipNetwork>().HasQueryFilter(e => !e.IsDeleted);
    }

    /// <summary>
    /// 保存更改时的处理
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>受影响的行数</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 处理审计字段
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
