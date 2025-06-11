using FluentAssertions;
using NovelManagement.Core.Entities;
using Xunit;

namespace NovelManagement.Core.Tests.Entities;

/// <summary>
/// BaseEntity 测试类
/// </summary>
public class BaseEntityTests
{
    /// <summary>
    /// 测试实体类，继承自BaseEntity
    /// </summary>
    private class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void BaseEntity_ShouldHaveDefaultValues_WhenCreated()
    {
        // Arrange & Act
        var entity = new TestEntity();

        // Assert
        entity.Id.Should().NotBe(Guid.Empty);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        entity.IsDeleted.Should().BeFalse();
        entity.CreatedBy.Should().BeNull();
        entity.UpdatedBy.Should().BeNull();
        entity.DeletedAt.Should().BeNull();
        entity.DeletedBy.Should().BeNull();
        entity.Version.Should().BeNull();
    }

    [Fact]
    public void BaseEntity_ShouldAllowSettingProperties()
    {
        // Arrange
        var entity = new TestEntity();
        var testId = Guid.NewGuid();
        var testDate = DateTime.UtcNow.AddDays(-1);
        var testUser = "TestUser";

        // Act
        entity.Id = testId;
        entity.CreatedAt = testDate;
        entity.UpdatedAt = testDate;
        entity.CreatedBy = testUser;
        entity.UpdatedBy = testUser;
        entity.IsDeleted = true;
        entity.DeletedAt = testDate;
        entity.DeletedBy = testUser;

        // Assert
        entity.Id.Should().Be(testId);
        entity.CreatedAt.Should().Be(testDate);
        entity.UpdatedAt.Should().Be(testDate);
        entity.CreatedBy.Should().Be(testUser);
        entity.UpdatedBy.Should().Be(testUser);
        entity.IsDeleted.Should().BeTrue();
        entity.DeletedAt.Should().Be(testDate);
        entity.DeletedBy.Should().Be(testUser);
    }
}
