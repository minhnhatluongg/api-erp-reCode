using AutoMapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Mappings
{
    public class ServiceTypeMappingProfile : Profile
    {
        public ServiceTypeMappingProfile()
        {
            //Read Entity -> Dto
            CreateMap<Domain.Entities.Accounts_payable.ServiceType, DTOs.ServiceTypeDto>();

            //write CreateDto → Entity

            CreateMap<CreateServiceTypeDto, ServiceType>()
                .ForMember(dest => dest.Code,
                    opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name,
                    opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Description,
                    opt => opt.MapFrom(src => src.Description != null ? src.Description.Trim() : null))
                // Các field không có trong DTO → bỏ qua, Service sẽ tự gán
                .ForMember(dest => dest.ServiceTypeID, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.Ignore())
                .ForMember(dest => dest.Crt_User, opt => opt.Ignore())
                .ForMember(dest => dest.Crt_Date, opt => opt.Ignore())
                .ForMember(dest => dest.ChgeUser, opt => opt.Ignore())
                .ForMember(dest => dest.ChgeDate, opt => opt.Ignore());

            // ── UpdateDto → Entity (dùng Map(src, dest) để giữ ID gốc) ──────
            CreateMap<UpdateServiceTypeDto, ServiceType>()
                .ForMember(dest => dest.Code,
                    opt => opt.MapFrom(src => src.Code.Trim().ToUpper()))
                .ForMember(dest => dest.Name,
                    opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Description,
                    opt => opt.MapFrom(src => src.Description != null ? src.Description.Trim() : null))
                // Giữ nguyên các field audit — Service tự gán sau khi map
                .ForMember(dest => dest.ServiceTypeID, opt => opt.Ignore())
                .ForMember(dest => dest.Crt_User, opt => opt.Ignore())
                .ForMember(dest => dest.Crt_Date, opt => opt.Ignore())
                .ForMember(dest => dest.ChgeUser, opt => opt.Ignore())
                .ForMember(dest => dest.ChgeDate, opt => opt.Ignore());
        }
    }
}
