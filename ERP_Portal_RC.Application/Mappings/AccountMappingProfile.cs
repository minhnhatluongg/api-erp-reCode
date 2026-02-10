using AutoMapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;

namespace ERP_Portal_RC.Application.Mappings
{
    public class AccountMappingProfile : Profile
    {
        public AccountMappingProfile()
        {
            // ApplicationUser <-> UserDto
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.LoginName))
                .ReverseMap();

            // Kế thừa mapping từ UserDto cho Detail
            CreateMap<ApplicationUser, UserDetailDto>()
                .IncludeBase<ApplicationUser, UserDto>();

            // Mapping từ thực thể cũ (UserOnAp) sang DTO mới
            CreateMap<UserOnAp, UserDto>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.LoginName));
        }
    }
}