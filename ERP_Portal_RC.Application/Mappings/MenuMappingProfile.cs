using AutoMapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;
using static ERP_Portal_RC.Application.DTOs.MenuResponseViewDto;

namespace ERP_Portal_RC.Application.Mappings
{
    public class MenuMappingProfile : Profile
    {
        public MenuMappingProfile()
        {
            CreateMap<ApplicationToolMenu, MenuDto>().ReverseMap();
            CreateMap<MenuResponseDto, MenuResponseViewDto>().ReverseMap();
        }
    }
}