using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NovelManagement.Application.DTOs;
using NovelManagement.Application.Services;
using NovelManagement.Core.Entities;
using NovelManagement.Core.Interfaces;
using Xunit;

namespace NovelManagement.Tests
{
    /// <summary>
    /// 角色管理功能测试
    /// </summary>
    public class CharacterManagementTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICharacterRepository> _mockCharacterRepository;
        private readonly Mock<ICharacterRelationshipRepository> _mockRelationshipRepository;
        private readonly Mock<IPlotRepository> _mockPlotRepository;
        private readonly Mock<IFactionRepository> _mockFactionRepository;
        private readonly ILogger<CharacterService> _logger;
        private readonly CharacterService _characterService;

        public CharacterManagementTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCharacterRepository = new Mock<ICharacterRepository>();
            _mockRelationshipRepository = new Mock<ICharacterRelationshipRepository>();
            _mockPlotRepository = new Mock<IPlotRepository>();
            _mockFactionRepository = new Mock<IFactionRepository>();
            _logger = NullLogger<CharacterService>.Instance;

            // 设置UnitOfWork返回模拟的仓储
            _mockUnitOfWork.Setup(u => u.Characters).Returns(_mockCharacterRepository.Object);
            _mockUnitOfWork.Setup(u => u.CharacterRelationships).Returns(_mockRelationshipRepository.Object);
            _mockUnitOfWork.Setup(u => u.Plots).Returns(_mockPlotRepository.Object);
            _mockUnitOfWork.Setup(u => u.Factions).Returns(_mockFactionRepository.Object);

            _characterService = new CharacterService(_mockUnitOfWork.Object, _logger);
        }

        [Fact]
        public async Task CheckCharacterReferencesAsync_WhenCharacterNotReferenced_ShouldReturnNotReferenced()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            
            _mockRelationshipRepository.Setup(r => r.GetByCharacterIdAsync(characterId, default))
                .ReturnsAsync(new List<CharacterRelationship>());
            
            _mockPlotRepository.Setup(p => p.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Plot, bool>>>(), default))
                .ReturnsAsync(new List<Plot>());

            _mockCharacterRepository.Setup(c => c.GetByIdAsync(characterId, default))
                .ReturnsAsync(new Character { Id = characterId, FactionId = null });

            // Act
            var result = await _characterService.CheckCharacterReferencesAsync(characterId);

            // Assert
            Assert.False(result.IsReferenced);
            Assert.Empty(result.References);
        }

        [Fact]
        public async Task CheckCharacterReferencesAsync_WhenCharacterHasRelationships_ShouldReturnReferenced()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var relationships = new List<CharacterRelationship>
            {
                new CharacterRelationship { Id = Guid.NewGuid(), SourceCharacterId = characterId },
                new CharacterRelationship { Id = Guid.NewGuid(), TargetCharacterId = characterId }
            };

            _mockRelationshipRepository.Setup(r => r.GetByCharacterIdAsync(characterId, default))
                .ReturnsAsync(relationships);
            
            _mockPlotRepository.Setup(p => p.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Plot, bool>>>(), default))
                .ReturnsAsync(new List<Plot>());

            _mockCharacterRepository.Setup(c => c.GetByIdAsync(characterId, default))
                .ReturnsAsync(new Character { Id = characterId, FactionId = null });

            // Act
            var result = await _characterService.CheckCharacterReferencesAsync(characterId);

            // Assert
            Assert.True(result.IsReferenced);
            Assert.Contains("存在 2 个角色关系", result.References);
        }

        [Fact]
        public async Task SafeDeleteCharacterAsync_WhenCharacterNotExists_ShouldReturnFailure()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            
            _mockCharacterRepository.Setup(c => c.GetByIdAsync(characterId, default))
                .ReturnsAsync((Character?)null);

            // Act
            var result = await _characterService.SafeDeleteCharacterAsync(characterId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("角色不存在", result.Message);
        }

        [Fact]
        public async Task SafeDeleteCharacterAsync_WhenCharacterIsReferenced_ShouldReturnFailure()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var character = new Character { Id = characterId, Name = "测试角色" };
            var relationships = new List<CharacterRelationship>
            {
                new CharacterRelationship { Id = Guid.NewGuid(), SourceCharacterId = characterId }
            };

            _mockCharacterRepository.Setup(c => c.GetByIdAsync(characterId, default))
                .ReturnsAsync(character);
            
            _mockRelationshipRepository.Setup(r => r.GetByCharacterIdAsync(characterId, default))
                .ReturnsAsync(relationships);
            
            _mockPlotRepository.Setup(p => p.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Plot, bool>>>(), default))
                .ReturnsAsync(new List<Plot>());

            // Act
            var result = await _characterService.SafeDeleteCharacterAsync(characterId);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("角色已被引用，无法删除", result.Message);
            Assert.NotNull(result.ReferenceInfo);
            Assert.True(result.ReferenceInfo.IsReferenced);
        }

        [Fact]
        public async Task SafeDeleteCharacterAsync_WhenCharacterNotReferenced_ShouldReturnSuccess()
        {
            // Arrange
            var characterId = Guid.NewGuid();
            var character = new Character { Id = characterId, Name = "测试角色" };

            _mockCharacterRepository.Setup(c => c.GetByIdAsync(characterId, default))
                .ReturnsAsync(character);
            
            _mockRelationshipRepository.Setup(r => r.GetByCharacterIdAsync(characterId, default))
                .ReturnsAsync(new List<CharacterRelationship>());
            
            _mockPlotRepository.Setup(p => p.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Plot, bool>>>(), default))
                .ReturnsAsync(new List<Plot>());

            _mockCharacterRepository.Setup(c => c.DeleteAsync(character, default))
                .Returns(Task.CompletedTask);
            
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync(default))
                .ReturnsAsync(1);

            // Act
            var result = await _characterService.SafeDeleteCharacterAsync(characterId);

            // Assert
            Assert.True(result.Success);
            Assert.Equal("角色删除成功", result.Message);
            
            // 验证删除操作被调用
            _mockCharacterRepository.Verify(c => c.DeleteAsync(character, default), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public void CharacterReferenceInfo_ShouldCalculateReferenceCountCorrectly()
        {
            // Arrange
            var referenceInfo = new CharacterReferenceInfo
            {
                CharacterId = Guid.NewGuid(),
                IsReferenced = true,
                References = new List<string>
                {
                    "存在 2 个角色关系",
                    "在 1 个剧情中被引用",
                    "属于势力 '玄天宗'"
                }
            };

            // Act & Assert
            Assert.Equal(3, referenceInfo.ReferenceCount);
        }

        [Fact]
        public void CharacterDeleteResult_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var result = new CharacterDeleteResult
            {
                Success = true,
                Message = "删除成功"
            };

            // Assert
            Assert.True(result.Success);
            Assert.Equal("删除成功", result.Message);
            Assert.Null(result.ReferenceInfo);
        }
    }
}
