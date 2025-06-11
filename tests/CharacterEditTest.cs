using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NovelManagement.Core.Entities;
using NovelManagement.Application.Services;
using NovelManagement.Infrastructure.Data;
using NovelManagement.Infrastructure.Data.UnitOfWork;

namespace NovelManagement.Tests
{
    /// <summary>
    /// 角色编辑功能测试
    /// </summary>
    public class CharacterEditTest
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly CharacterService _characterService;
        private readonly ProjectService _projectService;

        public CharacterEditTest()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
            
            _characterService = _serviceProvider.GetRequiredService<CharacterService>();
            _projectService = _serviceProvider.GetRequiredService<ProjectService>();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 添加日志
            services.AddLogging(builder => builder.AddConsole());

            // 添加数据库上下文（使用内存数据库进行测试）
            services.AddDbContext<NovelManagementDbContext>(options =>
                options.UseInMemoryDatabase("CharacterEditTest_" + Guid.NewGuid()));

            // 添加仓储和工作单元
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // 添加应用服务
            services.AddScoped<ProjectService>();
            services.AddScoped<CharacterService>();
        }

        /// <summary>
        /// 测试角色编辑功能
        /// </summary>
        public async Task<bool> TestCharacterEditAsync()
        {
            try
            {
                Console.WriteLine("开始测试角色编辑功能...");

                // 1. 创建测试项目
                var project = new Project
                {
                    Id = Guid.NewGuid(),
                    Name = "测试项目",
                    Description = "用于测试角色编辑的项目",
                    Type = "小说",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdProject = await _projectService.CreateProjectAsync(project);
                Console.WriteLine($"✅ 创建测试项目成功: {createdProject.Name}");

                // 2. 创建测试角色
                var character = new Character
                {
                    Id = Guid.NewGuid(),
                    ProjectId = createdProject.Id,
                    Name = "测试角色",
                    Type = "主角",
                    Age = 20,
                    Gender = "男",
                    Appearance = "英俊潇洒",
                    Personality = "勇敢正义",
                    Background = "出身平凡，机缘巧合下踏上修仙之路",
                    Tags = "势力:青云门,种族:人族",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "Active",
                    Importance = 1
                };

                var createdCharacter = await _characterService.CreateCharacterAsync(character);
                Console.WriteLine($"✅ 创建测试角色成功: {createdCharacter.Name}");

                // 3. 编辑角色信息（这是关键测试点）
                createdCharacter.Name = "修改后的角色名称";
                createdCharacter.Background = "修改后的背景故事";
                createdCharacter.Appearance = "修改后的外貌描述";
                createdCharacter.Tags = "势力:天剑宗,种族:人族"; // 修改势力信息

                var updatedCharacter = await _characterService.UpdateCharacterAsync(createdCharacter);
                Console.WriteLine($"✅ 角色编辑成功: {updatedCharacter.Name}");

                // 4. 验证修改是否生效
                var retrievedCharacter = await _characterService.GetCharacterByIdAsync(updatedCharacter.Id);
                if (retrievedCharacter == null)
                {
                    Console.WriteLine("❌ 无法获取更新后的角色");
                    return false;
                }

                if (retrievedCharacter.Name != "修改后的角色名称")
                {
                    Console.WriteLine($"❌ 角色名称更新失败，期望: 修改后的角色名称，实际: {retrievedCharacter.Name}");
                    return false;
                }

                if (retrievedCharacter.Background != "修改后的背景故事")
                {
                    Console.WriteLine($"❌ 角色背景更新失败，期望: 修改后的背景故事，实际: {retrievedCharacter.Background}");
                    return false;
                }

                if (retrievedCharacter.Tags != "势力:天剑宗,种族:人族")
                {
                    Console.WriteLine($"❌ 角色标签更新失败，期望: 势力:天剑宗,种族:人族，实际: {retrievedCharacter.Tags}");
                    return false;
                }

                Console.WriteLine("✅ 所有验证通过，角色编辑功能正常");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试失败: {ex.Message}");
                Console.WriteLine($"详细错误: {ex}");
                return false;
            }
        }

        /// <summary>
        /// 测试多次编辑同一角色（重点测试实体跟踪冲突修复）
        /// </summary>
        public async Task<bool> TestMultipleEditsAsync()
        {
            try
            {
                Console.WriteLine("开始测试多次编辑同一角色...");

                // 1. 创建测试项目
                var project = new Project
                {
                    Id = Guid.NewGuid(),
                    Name = "多次编辑测试项目",
                    Description = "用于测试多次编辑的项目",
                    Type = "小说",
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var createdProject = await _projectService.CreateProjectAsync(project);

                // 2. 创建测试角色
                var character = new Character
                {
                    Id = Guid.NewGuid(),
                    ProjectId = createdProject.Id,
                    Name = "多次编辑测试角色",
                    Type = "主角",
                    Background = "初始背景",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "Active",
                    Importance = 1
                };

                var createdCharacter = await _characterService.CreateCharacterAsync(character);

                // 3. 进行多次编辑（这是关键测试点）
                for (int i = 1; i <= 5; i++)
                {
                    createdCharacter.Background = $"第{i}次修改的背景";
                    createdCharacter.Appearance = $"第{i}次修改的外貌";
                    
                    var updatedCharacter = await _characterService.UpdateCharacterAsync(createdCharacter);
                    Console.WriteLine($"✅ 第{i}次编辑成功");
                    
                    // 更新引用以便下次编辑
                    createdCharacter = updatedCharacter;
                }

                Console.WriteLine("✅ 多次编辑测试通过，无实体跟踪冲突");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 多次编辑测试失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public async Task<bool> RunAllTestsAsync()
        {
            Console.WriteLine("=== 角色编辑功能测试开始 ===");
            
            bool test1 = await TestCharacterEditAsync();
            bool test2 = await TestMultipleEditsAsync();
            
            bool allPassed = test1 && test2;
            
            Console.WriteLine($"=== 测试结果: {(allPassed ? "✅ 全部通过" : "❌ 存在失败")} ===");
            
            return allPassed;
        }
    }
}
