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
    public class TechnicalMappingProfile : Profile
    {
        public TechnicalMappingProfile()
        {
            CreateMap<TechnicalRegistrationRequest, TechnicalUser>()
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username))
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.LastLogin, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedCodes, opt => opt.Ignore());
            CreateMap<TechnicalUser, LoginResponseDto>()
                .ForMember(dest => dest.AccessToken, opt => opt.Ignore())
                .ForMember(dest => dest.RefreshToken, opt => opt.Ignore())
                .ForMember(dest => dest.ExpiresAt, opt => opt.Ignore())
                .PreserveReferences();
            //Sau này cần map thêm các DTO khác liên quan đến TechnicalUser thì thêm ở đây
            CreateMap<RegistertrationCodes, RegistrationResultDto>()
                .PreserveReferences();

        }
    }
}
