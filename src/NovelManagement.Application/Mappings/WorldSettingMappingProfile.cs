using AutoMapper;
using NovelManagement.Application.DTOs;
using NovelManagement.Core.Entities;

namespace NovelManagement.Application.Mappings;

/// <summary>
/// 世界设定映射配置
/// </summary>
public class WorldSettingMappingProfile : Profile
{
    public WorldSettingMappingProfile()
    {
        // WorldSetting -> WorldSettingDto
        CreateMap<WorldSetting, WorldSettingDto>()
            .ForMember(dest => dest.ParentName, opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null))
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children));

        // CreateWorldSettingDto -> WorldSetting
        CreateMap<CreateWorldSettingDto, WorldSetting>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore());

        // UpdateWorldSettingDto -> WorldSetting
        CreateMap<UpdateWorldSettingDto, WorldSetting>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.Parent, opt => opt.Ignore())
            .ForMember(dest => dest.Children, opt => opt.Ignore());
    }
}

/// <summary>
/// 修炼体系映射配置
/// </summary>
public class CultivationSystemMappingProfile : Profile
{
    public CultivationSystemMappingProfile()
    {
        // CultivationSystem -> CultivationSystemDto
        CreateMap<CultivationSystem, CultivationSystemDto>()
            .ForMember(dest => dest.Levels, opt => opt.MapFrom(src => src.Levels));

        // CreateCultivationSystemDto -> CultivationSystem
        CreateMap<CreateCultivationSystemDto, CultivationSystem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.Levels, opt => opt.Ignore());

        // UpdateCultivationSystemDto -> CultivationSystem
        CreateMap<UpdateCultivationSystemDto, CultivationSystem>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Project, opt => opt.Ignore())
            .ForMember(dest => dest.Levels, opt => opt.Ignore());

        // CultivationLevel -> CultivationLevelDto
        CreateMap<CultivationLevel, CultivationLevelDto>();

        // CreateCultivationLevelDto -> CultivationLevel
        CreateMap<CreateCultivationLevelDto, CultivationLevel>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CultivationSystem, opt => opt.Ignore());

        // UpdateCultivationLevelDto -> CultivationLevel
        CreateMap<UpdateCultivationLevelDto, CultivationLevel>()
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CultivationSystem, opt => opt.Ignore());
    }
}
