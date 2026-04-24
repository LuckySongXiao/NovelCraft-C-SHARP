using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NovelManagement.WPF.Models;

namespace NovelManagement.WPF.Services
{
    /// <summary>
    /// 统一装配 AI 功能所需的项目上下文数据。
    /// </summary>
    public class ProjectContextAssembler
    {
        private readonly ProjectContextService _projectContextService;
        private readonly ProjectReadModelService _projectReadModelService;
        private readonly ILogger<ProjectContextAssembler> _logger;

        public ProjectContextAssembler(
            ProjectContextService projectContextService,
            ProjectReadModelService projectReadModelService,
            ILogger<ProjectContextAssembler> logger)
        {
            _projectContextService = projectContextService;
            _projectReadModelService = projectReadModelService;
            _logger = logger;
        }

        /// <summary>
        /// 基于当前项目上下文组装 AI 所需数据。
        /// </summary>
        public async Task<ProjectContextData> BuildCurrentProjectContextAsync()
        {
            var contextData = new ProjectContextData();

            try
            {
                if (_projectContextService.CurrentProjectId == null)
                {
                    return contextData;
                }

                var projectId = _projectContextService.CurrentProjectId.Value;
                contextData = await _projectReadModelService.BuildAiContextDataAsync(projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "构建项目上下文数据失败");
            }

            return contextData;
        }
    }
}
