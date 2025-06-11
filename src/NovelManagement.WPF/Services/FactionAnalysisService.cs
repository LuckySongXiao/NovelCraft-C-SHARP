using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.Application.DTOs;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 势力分析服务
    /// </summary>
    public class FactionAnalysisService
    {
        #region 字段和属性

        private readonly ILogger<FactionAnalysisService>? _logger;
        private readonly AICacheService _cacheService;

        /// <summary>
        /// 势力分析结果
        /// </summary>
        public class FactionAnalysisResult
        {
            public string FactionName { get; set; } = string.Empty;
            public double PowerScore { get; set; }
            public double InfluenceScore { get; set; }
            public double StabilityScore { get; set; }
            public double ThreatLevel { get; set; }
            public double OverallScore => (PowerScore + InfluenceScore + StabilityScore) / 3;
            
            public List<string> Strengths { get; set; } = new();
            public List<string> Weaknesses { get; set; } = new();
            public List<string> Opportunities { get; set; } = new();
            public List<string> Threats { get; set; } = new();
            
            public List<RelationshipAnalysis> Relationships { get; set; } = new();
            public List<ConflictPrediction> ConflictPredictions { get; set; } = new();
            public List<string> StrategicRecommendations { get; set; } = new();
            
            public DateTime AnalyzedAt { get; set; }
            public string ThinkingChainId { get; set; } = string.Empty;
        }

        /// <summary>
        /// 关系分析
        /// </summary>
        public class RelationshipAnalysis
        {
            public string TargetFactionName { get; set; } = string.Empty;
            public string RelationshipType { get; set; } = string.Empty; // 盟友、敌对、中立、竞争
            public double RelationshipStrength { get; set; }
            public string Description { get; set; } = string.Empty;
            public List<string> KeyFactors { get; set; } = new();
        }

        /// <summary>
        /// 冲突预测
        /// </summary>
        public class ConflictPrediction
        {
            public string OpponentFactionName { get; set; } = string.Empty;
            public double ConflictProbability { get; set; }
            public string ConflictType { get; set; } = string.Empty; // 军事、经济、政治、资源
            public string TimeFrame { get; set; } = string.Empty; // 短期、中期、长期
            public string Trigger { get; set; } = string.Empty;
            public List<string> ConsequencePredictions { get; set; } = new();
        }

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logger">日志记录器</param>
        /// <param name="cacheService">缓存服务</param>
        public FactionAnalysisService(ILogger<FactionAnalysisService>? logger = null, AICacheService? cacheService = null)
        {
            _logger = logger;
            // 为AICacheService创建专用的Logger
            var cacheLogger = App.ServiceProvider?.GetService(typeof(ILogger<AICacheService>)) as ILogger<AICacheService>;
            _cacheService = cacheService ?? new AICacheService(cacheLogger);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 分析单个势力
        /// </summary>
        /// <param name="faction">要分析的势力</param>
        /// <param name="allFactions">所有势力列表（用于关系分析）</param>
        /// <returns>分析结果</returns>
        public async Task<FactionAnalysisResult> AnalyzeFactionAsync(FactionDto faction, List<FactionDto> allFactions)
        {
            try
            {
                _logger?.LogInformation("开始分析势力: {FactionName}", faction.Name);

                // 生成缓存键
                var factionData = $"{faction.Name}_{faction.Type}_{faction.PowerLevel}_{faction.MemberCount}";
                var cacheKey = AICacheKeyGenerator.GenerateFactionAnalysisKey(faction.Id.ToString(), factionData);

                // 尝试从缓存获取结果
                var cachedResult = _cacheService.Get<FactionAnalysisResult>(cacheKey);
                if (cachedResult != null)
                {
                    _logger?.LogInformation("从缓存获取势力分析结果: {FactionName}", faction.Name);
                    return cachedResult;
                }

                // 缓存未命中，执行新分析
                _logger?.LogInformation("缓存未命中，开始新的势力分析: {FactionName}", faction.Name);

                // 模拟AI分析过程
                await Task.Delay(3000);

                var result = new FactionAnalysisResult
                {
                    FactionName = faction.Name,
                    AnalyzedAt = DateTime.Now,
                    ThinkingChainId = Guid.NewGuid().ToString()
                };

                // 分析势力基础属性
                await AnalyzeFactionPower(faction, result);
                await AnalyzeFactionInfluence(faction, result);
                await AnalyzeFactionStability(faction, result);

                // 分析势力关系
                await AnalyzeFactionRelationships(faction, allFactions, result);

                // 预测潜在冲突
                await PredictPotentialConflicts(faction, allFactions, result);

                // 生成SWOT分析
                await GenerateSWOTAnalysis(faction, allFactions, result);

                // 生成战略建议
                await GenerateStrategicRecommendations(faction, result);

                // 将结果存入缓存（缓存1小时）
                _cacheService.Set(cacheKey, result, TimeSpan.FromHours(1));

                _logger?.LogInformation("势力分析完成: {FactionName}, 总体评分: {Score:F1}", 
                    faction.Name, result.OverallScore);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "分析势力失败: {FactionName}", faction.Name);
                throw;
            }
        }

        /// <summary>
        /// 批量分析多个势力的关系网络
        /// </summary>
        /// <param name="factions">势力列表</param>
        /// <returns>网络分析结果</returns>
        public async Task<NetworkAnalysisResult> AnalyzeFactionNetworkAsync(List<FactionDto> factions)
        {
            try
            {
                _logger?.LogInformation("开始分析势力网络，势力数量: {Count}", factions.Count);

                // 模拟网络分析
                await Task.Delay(2000);

                var result = new NetworkAnalysisResult
                {
                    TotalFactions = factions.Count,
                    AnalyzedAt = DateTime.Now
                };

                // 分析网络结构
                await AnalyzeNetworkStructure(factions, result);

                // 识别关键节点
                await IdentifyKeyNodes(factions, result);

                // 分析权力平衡
                await AnalyzePowerBalance(factions, result);

                _logger?.LogInformation("势力网络分析完成");
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "势力网络分析失败");
                throw;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 分析势力实力
        /// </summary>
        private async Task AnalyzeFactionPower(FactionDto faction, FactionAnalysisResult result)
        {
            await Task.Delay(100);

            var baseScore = 5.0;

            // 根据势力等级评分
            baseScore += faction.PowerLevel switch
            {
                "超级" => 4.0,
                "一流" => 3.0,
                "二流" => 2.0,
                "三流" => 1.0,
                _ => 0.0
            };

            // 根据成员数量调整
            if (faction.MemberCount > 10000) baseScore += 1.0;
            else if (faction.MemberCount > 5000) baseScore += 0.5;
            else if (faction.MemberCount < 1000) baseScore -= 0.5;

            result.PowerScore = Math.Max(1.0, Math.Min(10.0, baseScore));
        }

        /// <summary>
        /// 分析势力影响力
        /// </summary>
        private async Task AnalyzeFactionInfluence(FactionDto faction, FactionAnalysisResult result)
        {
            await Task.Delay(100);

            var baseScore = 6.0;

            // 根据势力类型调整影响力
            baseScore += faction.Type switch
            {
                "国家" => 2.0,
                "宗门" => 1.5,
                "商会" => 1.0,
                "家族" => 0.5,
                _ => 0.0
            };

            // 根据描述内容分析影响力关键词
            var description = faction.Description?.ToLower() ?? "";
            if (description.Contains("统治") || description.Contains("控制")) baseScore += 1.0;
            if (description.Contains("影响") || description.Contains("声望")) baseScore += 0.5;

            result.InfluenceScore = Math.Max(1.0, Math.Min(10.0, baseScore));
        }

        /// <summary>
        /// 分析势力稳定性
        /// </summary>
        private async Task AnalyzeFactionStability(FactionDto faction, FactionAnalysisResult result)
        {
            await Task.Delay(100);

            var baseScore = 7.0;

            // 根据势力历史分析稳定性
            var history = faction.History?.ToLower() ?? "";
            if (history.Contains("分裂") || history.Contains("内乱")) baseScore -= 2.0;
            if (history.Contains("统一") || history.Contains("稳定")) baseScore += 1.0;
            if (history.Contains("传承") || history.Contains("悠久")) baseScore += 0.5;

            // 根据势力类型调整稳定性
            baseScore += faction.Type switch
            {
                "家族" => 1.0,
                "宗门" => 0.5,
                "国家" => 0.0,
                "组织" => -0.5,
                _ => 0.0
            };

            result.StabilityScore = Math.Max(1.0, Math.Min(10.0, baseScore));
        }

        /// <summary>
        /// 分析势力关系
        /// </summary>
        private async Task AnalyzeFactionRelationships(FactionDto faction, List<FactionDto> allFactions, FactionAnalysisResult result)
        {
            await Task.Delay(200);

            var otherFactions = allFactions.Where(f => f.Id != faction.Id).ToList();

            foreach (var otherFaction in otherFactions)
            {
                var relationship = await AnalyzeBilateralRelationship(faction, otherFaction);
                if (relationship != null)
                {
                    result.Relationships.Add(relationship);
                }
            }

            _logger?.LogDebug("分析了 {Count} 个势力关系", result.Relationships.Count);
        }

        /// <summary>
        /// 分析双边关系
        /// </summary>
        private async Task<RelationshipAnalysis?> AnalyzeBilateralRelationship(FactionDto faction1, FactionDto faction2)
        {
            await Task.Delay(50);

            var relationship = new RelationshipAnalysis
            {
                TargetFactionName = faction2.Name
            };

            // 基于势力类型分析关系
            var relationshipType = DetermineRelationshipType(faction1, faction2);
            relationship.RelationshipType = relationshipType;

            // 计算关系强度
            relationship.RelationshipStrength = CalculateRelationshipStrength(faction1, faction2, relationshipType);

            // 生成关系描述
            relationship.Description = GenerateRelationshipDescription(faction1, faction2, relationshipType);

            // 分析关键因素
            relationship.KeyFactors = AnalyzeRelationshipFactors(faction1, faction2);

            return relationship;
        }

        /// <summary>
        /// 确定关系类型
        /// </summary>
        private string DetermineRelationshipType(FactionDto faction1, FactionDto faction2)
        {
            // 基于势力类型的关系矩阵
            var typeRelations = new Dictionary<(string, string), string>
            {
                { ("宗门", "宗门"), "竞争" },
                { ("家族", "家族"), "中立" },
                { ("国家", "国家"), "敌对" },
                { ("商会", "商会"), "竞争" },
                { ("宗门", "家族"), "中立" },
                { ("国家", "宗门"), "合作" },
                { ("商会", "国家"), "合作" }
            };

            var key1 = (faction1.Type, faction2.Type);
            var key2 = (faction2.Type, faction1.Type);

            if (typeRelations.ContainsKey(key1))
                return typeRelations[key1];
            if (typeRelations.ContainsKey(key2))
                return typeRelations[key2];

            // 基于实力等级判断
            if (faction1.PowerLevel == "超级" && faction2.PowerLevel == "超级")
                return "敌对";
            if (faction1.PowerLevel == "超级" || faction2.PowerLevel == "超级")
                return "威胁";

            return "中立";
        }

        /// <summary>
        /// 计算关系强度
        /// </summary>
        private double CalculateRelationshipStrength(FactionDto faction1, FactionDto faction2, string relationshipType)
        {
            var baseStrength = relationshipType switch
            {
                "盟友" => 8.0,
                "合作" => 6.0,
                "中立" => 4.0,
                "竞争" => 6.0,
                "敌对" => 8.0,
                "威胁" => 7.0,
                _ => 4.0
            };

            // 根据实力差距调整
            var powerDiff = Math.Abs(GetPowerLevel(faction1.PowerLevel) - GetPowerLevel(faction2.PowerLevel));
            if (powerDiff > 2) baseStrength += 1.0; // 实力差距大，关系更紧张或更明确

            return Math.Max(1.0, Math.Min(10.0, baseStrength));
        }

        /// <summary>
        /// 获取实力等级数值
        /// </summary>
        private int GetPowerLevel(string powerLevel)
        {
            return powerLevel switch
            {
                "超级" => 5,
                "一流" => 4,
                "二流" => 3,
                "三流" => 2,
                "普通" => 1,
                _ => 1
            };
        }

        /// <summary>
        /// 生成关系描述
        /// </summary>
        private string GenerateRelationshipDescription(FactionDto faction1, FactionDto faction2, string relationshipType)
        {
            return relationshipType switch
            {
                "盟友" => $"{faction1.Name}与{faction2.Name}保持着稳固的联盟关系，在多个领域展开深度合作。",
                "合作" => $"{faction1.Name}与{faction2.Name}在特定领域存在合作关系，互利共赢。",
                "中立" => $"{faction1.Name}与{faction2.Name}保持中立态度，较少直接接触。",
                "竞争" => $"{faction1.Name}与{faction2.Name}在相同领域存在激烈竞争，争夺资源和影响力。",
                "敌对" => $"{faction1.Name}与{faction2.Name}存在根本性利益冲突，关系紧张。",
                "威胁" => $"{faction1.Name}将{faction2.Name}视为潜在威胁，需要密切关注其动向。",
                _ => $"{faction1.Name}与{faction2.Name}的关系尚不明确，需要进一步观察。"
            };
        }

        /// <summary>
        /// 分析关系关键因素
        /// </summary>
        private List<string> AnalyzeRelationshipFactors(FactionDto faction1, FactionDto faction2)
        {
            var factors = new List<string>();

            // 地理因素
            factors.Add("地理位置影响");

            // 资源因素
            if (faction1.Type == "商会" || faction2.Type == "商会")
                factors.Add("经济利益");

            // 实力因素
            var power1 = GetPowerLevel(faction1.PowerLevel);
            var power2 = GetPowerLevel(faction2.PowerLevel);
            if (Math.Abs(power1 - power2) > 1)
                factors.Add("实力差距");

            // 类型因素
            if (faction1.Type == faction2.Type)
                factors.Add("同类竞争");

            return factors;
        }

        /// <summary>
        /// 预测潜在冲突
        /// </summary>
        private async Task PredictPotentialConflicts(FactionDto faction, List<FactionDto> allFactions, FactionAnalysisResult result)
        {
            await Task.Delay(300);

            var otherFactions = allFactions.Where(f => f.Id != faction.Id).ToList();

            foreach (var otherFaction in otherFactions)
            {
                var conflictPrediction = await PredictBilateralConflict(faction, otherFaction);
                if (conflictPrediction != null && conflictPrediction.ConflictProbability > 0.3) // 只保留概率大于30%的冲突
                {
                    result.ConflictPredictions.Add(conflictPrediction);
                }
            }

            // 按冲突概率排序
            result.ConflictPredictions = result.ConflictPredictions.OrderByDescending(c => c.ConflictProbability).ToList();

            _logger?.LogDebug("预测了 {Count} 个潜在冲突", result.ConflictPredictions.Count);
        }

        /// <summary>
        /// 预测双边冲突
        /// </summary>
        private async Task<ConflictPrediction?> PredictBilateralConflict(FactionDto faction1, FactionDto faction2)
        {
            await Task.Delay(50);

            var prediction = new ConflictPrediction
            {
                OpponentFactionName = faction2.Name
            };

            // 计算冲突概率
            prediction.ConflictProbability = CalculateConflictProbability(faction1, faction2);

            // 确定冲突类型
            prediction.ConflictType = DetermineConflictType(faction1, faction2);

            // 预测时间框架
            prediction.TimeFrame = PredictTimeFrame(faction1, faction2, prediction.ConflictProbability);

            // 识别冲突触发因素
            prediction.Trigger = IdentifyConflictTrigger(faction1, faction2);

            // 预测后果
            prediction.ConsequencePredictions = PredictConflictConsequences(faction1, faction2, prediction.ConflictType);

            return prediction;
        }

        /// <summary>
        /// 计算冲突概率
        /// </summary>
        private double CalculateConflictProbability(FactionDto faction1, FactionDto faction2)
        {
            var baseProbability = 0.2; // 基础概率20%

            // 基于关系类型调整
            var relationshipType = DetermineRelationshipType(faction1, faction2);
            baseProbability += relationshipType switch
            {
                "敌对" => 0.5,
                "竞争" => 0.3,
                "威胁" => 0.4,
                "中立" => 0.1,
                "合作" => -0.1,
                "盟友" => -0.15,
                _ => 0.0
            };

            // 基于实力等级调整
            if (faction1.PowerLevel == "超级" && faction2.PowerLevel == "超级")
                baseProbability += 0.2; // 超级势力间更容易冲突

            // 基于势力类型调整
            if (faction1.Type == faction2.Type)
                baseProbability += 0.15; // 同类型势力更容易冲突

            // 基于成员数量调整（资源竞争）
            if (faction1.MemberCount > 5000 && faction2.MemberCount > 5000)
                baseProbability += 0.1;

            return Math.Max(0.0, Math.Min(1.0, baseProbability));
        }

        /// <summary>
        /// 确定冲突类型
        /// </summary>
        private string DetermineConflictType(FactionDto faction1, FactionDto faction2)
        {
            // 基于势力类型确定主要冲突类型
            if (faction1.Type == "商会" || faction2.Type == "商会")
                return "经济";

            if (faction1.Type == "国家" || faction2.Type == "国家")
                return "政治";

            if (faction1.Type == "宗门" && faction2.Type == "宗门")
                return "资源";

            return "军事";
        }

        /// <summary>
        /// 预测时间框架
        /// </summary>
        private string PredictTimeFrame(FactionDto faction1, FactionDto faction2, double conflictProbability)
        {
            if (conflictProbability > 0.7)
                return "短期";
            else if (conflictProbability > 0.4)
                return "中期";
            else
                return "长期";
        }

        /// <summary>
        /// 识别冲突触发因素
        /// </summary>
        private string IdentifyConflictTrigger(FactionDto faction1, FactionDto faction2)
        {
            var triggers = new List<string>();

            if (faction1.Type == faction2.Type)
                triggers.Add("领域竞争");

            if (faction1.PowerLevel == "超级" || faction2.PowerLevel == "超级")
                triggers.Add("权力争夺");

            if (faction1.Type == "商会" || faction2.Type == "商会")
                triggers.Add("经济利益");

            return triggers.FirstOrDefault() ?? "未知因素";
        }

        /// <summary>
        /// 预测冲突后果
        /// </summary>
        private List<string> PredictConflictConsequences(FactionDto faction1, FactionDto faction2, string conflictType)
        {
            var consequences = new List<string>();

            switch (conflictType)
            {
                case "军事":
                    consequences.Add("可能导致大规模武装冲突");
                    consequences.Add("周边势力被迫选边站队");
                    consequences.Add("地区稳定性受到严重影响");
                    break;

                case "经济":
                    consequences.Add("贸易关系紧张");
                    consequences.Add("资源价格波动");
                    consequences.Add("经济联盟重新洗牌");
                    break;

                case "政治":
                    consequences.Add("外交关系恶化");
                    consequences.Add("政治影响力重新分配");
                    consequences.Add("国际秩序受到冲击");
                    break;

                case "资源":
                    consequences.Add("资源争夺加剧");
                    consequences.Add("供应链受到影响");
                    consequences.Add("新的资源联盟形成");
                    break;
            }

            return consequences;
        }

        /// <summary>
        /// 生成SWOT分析
        /// </summary>
        private async Task GenerateSWOTAnalysis(FactionDto faction, List<FactionDto> allFactions, FactionAnalysisResult result)
        {
            await Task.Delay(200);

            // 优势 (Strengths)
            result.Strengths = GenerateStrengths(faction);

            // 劣势 (Weaknesses)
            result.Weaknesses = GenerateWeaknesses(faction);

            // 机会 (Opportunities)
            result.Opportunities = GenerateOpportunities(faction, allFactions);

            // 威胁 (Threats)
            result.Threats = GenerateThreats(faction, allFactions);

            _logger?.LogDebug("生成SWOT分析完成");
        }

        /// <summary>
        /// 生成优势
        /// </summary>
        private List<string> GenerateStrengths(FactionDto faction)
        {
            var strengths = new List<string>();

            if (faction.PowerLevel == "超级")
                strengths.Add("拥有超级实力，在地区具有绝对优势");
            else if (faction.PowerLevel == "一流")
                strengths.Add("实力雄厚，在同类势力中处于领先地位");

            if (faction.MemberCount > 10000)
                strengths.Add("成员众多，人力资源充足");

            switch (faction.Type)
            {
                case "国家":
                    strengths.Add("拥有完整的政治体系和法律框架");
                    break;
                case "宗门":
                    strengths.Add("传承悠久，拥有深厚的文化底蕴");
                    break;
                case "商会":
                    strengths.Add("经济实力雄厚，商业网络发达");
                    break;
                case "家族":
                    strengths.Add("血缘纽带紧密，内部凝聚力强");
                    break;
            }

            if (strengths.Count == 0)
                strengths.Add("具备基础的组织结构和运营能力");

            return strengths;
        }

        /// <summary>
        /// 生成劣势
        /// </summary>
        private List<string> GenerateWeaknesses(FactionDto faction)
        {
            var weaknesses = new List<string>();

            if (faction.PowerLevel == "三流" || faction.PowerLevel == "普通")
                weaknesses.Add("实力相对较弱，影响力有限");

            if (faction.MemberCount < 1000)
                weaknesses.Add("成员数量不足，人力资源紧张");

            var history = faction.History?.ToLower() ?? "";
            if (history.Contains("分裂") || history.Contains("内乱"))
                weaknesses.Add("历史上存在内部分裂，稳定性存疑");

            switch (faction.Type)
            {
                case "组织":
                    weaknesses.Add("组织结构相对松散，缺乏强有力的约束机制");
                    break;
            }

            if (weaknesses.Count == 0)
                weaknesses.Add("在某些专业领域可能存在短板");

            return weaknesses;
        }

        /// <summary>
        /// 生成机会
        /// </summary>
        private List<string> GenerateOpportunities(FactionDto faction, List<FactionDto> allFactions)
        {
            var opportunities = new List<string>();

            // 分析潜在合作伙伴
            var potentialAllies = allFactions.Where(f => f.Id != faction.Id &&
                DetermineRelationshipType(faction, f) == "合作").ToList();

            if (potentialAllies.Any())
                opportunities.Add($"可与{potentialAllies.First().Name}等势力建立更深层次的合作关系");

            // 基于势力类型的机会
            switch (faction.Type)
            {
                case "商会":
                    opportunities.Add("经济发展为商业扩张提供了良好机遇");
                    break;
                case "宗门":
                    opportunities.Add("可通过传承文化和技艺扩大影响力");
                    break;
                case "国家":
                    opportunities.Add("可通过外交手段扩大政治影响力");
                    break;
            }

            // 基于实力等级的机会
            if (faction.PowerLevel != "超级")
                opportunities.Add("通过内部改革和发展有望提升实力等级");

            if (opportunities.Count == 0)
                opportunities.Add("在当前环境下存在发展和扩张的潜在机会");

            return opportunities;
        }

        /// <summary>
        /// 生成威胁
        /// </summary>
        private List<string> GenerateThreats(FactionDto faction, List<FactionDto> allFactions)
        {
            var threats = new List<string>();

            // 分析主要威胁
            var majorThreats = allFactions.Where(f => f.Id != faction.Id &&
                (DetermineRelationshipType(faction, f) == "敌对" ||
                 DetermineRelationshipType(faction, f) == "威胁")).ToList();

            foreach (var threat in majorThreats.Take(2)) // 只取前两个主要威胁
            {
                threats.Add($"{threat.Name}的{threat.PowerLevel}实力对本势力构成直接威胁");
            }

            // 基于实力等级的威胁
            if (faction.PowerLevel == "三流" || faction.PowerLevel == "普通")
                threats.Add("实力相对较弱，容易受到其他势力的压制");

            // 基于势力类型的威胁
            var sameTypeFactions = allFactions.Where(f => f.Id != faction.Id && f.Type == faction.Type).ToList();
            if (sameTypeFactions.Any())
                threats.Add($"同类型势力的竞争压力，特别是来自{sameTypeFactions.First().Name}的挑战");

            if (threats.Count == 0)
                threats.Add("外部环境变化可能对势力发展造成不利影响");

            return threats;
        }

        /// <summary>
        /// 生成战略建议
        /// </summary>
        private async Task GenerateStrategicRecommendations(FactionDto faction, FactionAnalysisResult result)
        {
            await Task.Delay(100);

            var recommendations = new List<string>();

            // 基于SWOT分析生成建议
            if (result.Strengths.Any(s => s.Contains("实力")))
                recommendations.Add("充分发挥实力优势，在关键领域建立主导地位");

            if (result.Weaknesses.Any(w => w.Contains("人力")))
                recommendations.Add("加强人才招募和培养，扩大组织规模");

            if (result.Opportunities.Any(o => o.Contains("合作")))
                recommendations.Add("积极寻求战略合作伙伴，建立互利共赢的联盟关系");

            if (result.Threats.Any(t => t.Contains("威胁")))
                recommendations.Add("制定防御策略，提高应对外部威胁的能力");

            // 基于冲突预测生成建议
            if (result.ConflictPredictions.Any(c => c.ConflictProbability > 0.6))
                recommendations.Add("高度关注潜在冲突，提前制定应对预案");

            // 基于势力类型生成建议
            switch (faction.Type)
            {
                case "商会":
                    recommendations.Add("扩大商业网络，多元化经营降低风险");
                    break;
                case "宗门":
                    recommendations.Add("传承核心技艺，培养下一代领导者");
                    break;
                case "国家":
                    recommendations.Add("完善治理体系，提高行政效率");
                    break;
                case "家族":
                    recommendations.Add("维护家族团结，合理分配权力和资源");
                    break;
            }

            if (recommendations.Count == 0)
                recommendations.Add("保持稳健发展，适时调整战略方向");

            result.StrategicRecommendations = recommendations;
        }

        /// <summary>
        /// 分析网络结构
        /// </summary>
        private async Task AnalyzeNetworkStructure(List<FactionDto> factions, NetworkAnalysisResult result)
        {
            await Task.Delay(100);

            var insights = new List<string>();

            // 分析势力类型分布
            var typeGroups = factions.GroupBy(f => f.Type).ToList();
            insights.Add($"网络包含{typeGroups.Count}种不同类型的势力");

            // 分析实力分布
            var powerGroups = factions.GroupBy(f => f.PowerLevel).ToList();
            var superPowers = factions.Count(f => f.PowerLevel == "超级");
            if (superPowers > 1)
                insights.Add($"存在{superPowers}个超级势力，可能形成多极格局");
            else if (superPowers == 1)
                insights.Add("存在单一超级势力，可能形成一超多强格局");

            // 分析网络密度
            var totalPossibleRelations = factions.Count * (factions.Count - 1) / 2;
            var actualRelations = 0;
            foreach (var faction1 in factions)
            {
                foreach (var faction2 in factions.Where(f => f.Id != faction1.Id))
                {
                    var relationType = DetermineRelationshipType(faction1, faction2);
                    if (relationType != "中立")
                        actualRelations++;
                }
            }
            var networkDensity = (double)actualRelations / totalPossibleRelations;
            insights.Add($"网络密度为{networkDensity:P1}，关系{(networkDensity > 0.5 ? "较为密切" : "相对松散")}");

            result.NetworkInsights = insights;
        }

        /// <summary>
        /// 识别关键节点
        /// </summary>
        private async Task IdentifyKeyNodes(List<FactionDto> factions, NetworkAnalysisResult result)
        {
            await Task.Delay(100);

            var keyNodes = new List<string>();

            // 基于实力等级识别
            var superPowers = factions.Where(f => f.PowerLevel == "超级").ToList();
            keyNodes.AddRange(superPowers.Select(f => f.Name));

            // 基于连接度识别（模拟）
            var highConnectivityFactions = factions
                .Where(f => f.Type == "商会" || f.Type == "国家") // 这些类型通常连接度较高
                .Take(2)
                .ToList();

            foreach (var faction in highConnectivityFactions)
            {
                if (!keyNodes.Contains(faction.Name))
                    keyNodes.Add(faction.Name);
            }

            result.KeyNodes = keyNodes;
        }

        /// <summary>
        /// 分析权力平衡
        /// </summary>
        private async Task AnalyzePowerBalance(List<FactionDto> factions, NetworkAnalysisResult result)
        {
            await Task.Delay(100);

            var powerCenters = new List<string>();

            // 识别权力中心
            var superPowers = factions.Where(f => f.PowerLevel == "超级").ToList();
            var firstTierPowers = factions.Where(f => f.PowerLevel == "一流").ToList();

            powerCenters.AddRange(superPowers.Select(f => f.Name));
            powerCenters.AddRange(firstTierPowers.Take(3).Select(f => f.Name)); // 最多取3个一流势力

            result.PowerCenters = powerCenters;

            // 计算网络稳定性
            var stabilityScore = CalculateNetworkStability(factions);
            result.NetworkStability = stabilityScore;
        }

        /// <summary>
        /// 计算网络稳定性
        /// </summary>
        private double CalculateNetworkStability(List<FactionDto> factions)
        {
            var baseStability = 7.0;

            // 超级势力数量对稳定性的影响
            var superPowerCount = factions.Count(f => f.PowerLevel == "超级");
            if (superPowerCount == 0)
                baseStability -= 2.0; // 没有强势力维持秩序
            else if (superPowerCount == 1)
                baseStability += 1.0; // 单极稳定
            else if (superPowerCount == 2)
                baseStability -= 1.0; // 双极对抗
            else
                baseStability -= 2.0; // 多极混乱

            // 势力类型多样性对稳定性的影响
            var typeCount = factions.Select(f => f.Type).Distinct().Count();
            if (typeCount > 3)
                baseStability += 0.5; // 多样性有利于平衡

            // 总体实力分布对稳定性的影响
            var averagePowerLevel = factions.Average(f => GetPowerLevel(f.PowerLevel));
            if (averagePowerLevel > 3.0)
                baseStability += 0.5; // 整体实力强有利于稳定

            return Math.Max(1.0, Math.Min(10.0, baseStability));
        }

        #endregion

        #region 嵌套类

        /// <summary>
        /// 网络分析结果
        /// </summary>
        public class NetworkAnalysisResult
        {
            public int TotalFactions { get; set; }
            public List<string> KeyNodes { get; set; } = new();
            public List<string> PowerCenters { get; set; } = new();
            public List<string> NetworkInsights { get; set; } = new();
            public double NetworkStability { get; set; }
            public DateTime AnalyzedAt { get; set; }
        }

        #endregion
    }
}
