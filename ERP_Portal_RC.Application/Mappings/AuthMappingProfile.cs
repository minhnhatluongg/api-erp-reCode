using AutoMapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Mappings
{
    public class AuthMappingProfile : Profile
    {
        public AuthMappingProfile()
        {
            // Simple mapping - ApplicationUser to UserDto
            // No longer need explicit mappings since we removed IdentityUser inheritance
            CreateMap<ApplicationUser, UserDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.LoginName));

            CreateMap<ApplicationUser, AuthResponseDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore()); // Will be set manually
        }
    }
}
