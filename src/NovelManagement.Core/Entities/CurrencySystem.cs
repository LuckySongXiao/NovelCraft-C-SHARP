using System.ComponentModel.DataAnnotations;
using NovelManagement.Core.Entities;

namespace NovelManagement.Core.Entities;

/// <summary>
/// 货币体系实体
/// </summary>
public class CurrencySystem : BaseEntity
{
    /// <summary>
    /// 体系名称
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 货币制度
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MonetarySystem { get; set; } = string.Empty; // 金本位制、银本位制、信用货币制、物物交换制、灵石货币制、混合货币制

    /// <summary>
    /// 货币类型
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// 货币状态
    /// </summary>
    [MaxLength(50)]
    public string? Status { get; set; }

    /// <summary>
    /// 货币稳定性（1-100）
    /// </summary>
    public int Stability { get; set; } = 50;

    /// <summary>
    /// 基础价值
    /// </summary>
    public decimal BaseValue { get; set; } = 1.0m;

    /// <summary>
    /// 基础货币
    /// </summary>
    [MaxLength(100)]
    public string? BaseCurrency { get; set; }

    /// <summary>
    /// 货币种类（JSON格式）
    /// </summary>
    public string? CurrencyTypes { get; set; }

    /// <summary>
    /// 汇率体系（JSON格式）
    /// </summary>
    public string? ExchangeRates { get; set; }

    /// <summary>
    /// 发行机构
    /// </summary>
    [MaxLength(200)]
    public string? IssuingAuthority { get; set; }

    /// <summary>
    /// 通胀率
    /// </summary>
    public decimal? InflationRate { get; set; }

    /// <summary>
    /// 利率水平
    /// </summary>
    public decimal? InterestRate { get; set; }

    /// <summary>
    /// 货币供应量
    /// </summary>
    public long? MoneySupply { get; set; }

    /// <summary>
    /// 汇率波动
    /// </summary>
    public decimal? ExchangeRateVolatility { get; set; }

    /// <summary>
    /// 金融服务（JSON格式）
    /// </summary>
    public string? FinancialServices { get; set; }

    /// <summary>
    /// 体系描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 历史背景
    /// </summary>
    public string? HistoricalBackground { get; set; }

    /// <summary>
    /// 所属项目ID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// 重要性等级（1-10）
    /// </summary>
    public int Importance { get; set; } = 5;

    /// <summary>
    /// 体系标签
    /// </summary>
    [MaxLength(500)]
    public string? Tags { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// 建立时间
    /// </summary>
    public DateTime? EstablishedDate { get; set; }

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime? LastUpdatedDate { get; set; }

    /// <summary>
    /// 是否活跃
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 使用范围
    /// </summary>
    [MaxLength(500)]
    public string? UsageScope { get; set; }

    /// <summary>
    /// 监管机构
    /// </summary>
    [MaxLength(200)]
    public string? RegulatoryAuthority { get; set; }

    /// <summary>
    /// 法律框架
    /// </summary>
    public string? LegalFramework { get; set; }

    /// <summary>
    /// 经济指标（JSON格式）
    /// </summary>
    public string? EconomicIndicators { get; set; }

    /// <summary>
    /// 风险评估
    /// </summary>
    public string? RiskAssessment { get; set; }

    // 导航属性
    /// <summary>
    /// 所属项目
    /// </summary>
    public virtual Project Project { get; set; } = null!;

    /// <summary>
    /// 使用该货币体系的势力
    /// </summary>
    public virtual ICollection<Faction> UsingFactions { get; set; } = new List<Faction>();
}
