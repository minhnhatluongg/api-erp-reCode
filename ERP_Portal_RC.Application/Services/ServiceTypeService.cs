using AutoMapper;
using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities.Accounts_payable;
using ERP_Portal_RC.Domain.Interfaces.Accounts_payable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class ServiceTypeService : IServiceTypeService
    {
        private readonly IServiceTypeRepository _repository;
        private readonly IMapper _mapper;
        public ServiceTypeService(IServiceTypeRepository serviceTypeRepository, IMapper mapper)
        {
            _repository = serviceTypeRepository;
            _mapper = mapper;
        }
        public async Task<int> CreateAsync(CreateServiceTypeDto dto, string crtUser)
        {
            if (await _repository.IsCodeExistsAsync(dto.Code))
                throw new InvalidOperationException($"Mã dịch vụ '{dto.Code.Trim().ToUpper()}' đã tồn tại.");

            // Map DTO → Entity -> sau đó gán các field audit thủ công
            var entity = _mapper.Map<ServiceType>(dto);
            entity.IsActive = true;

            entity.Crt_User = crtUser; //lấy ở jwt
            entity.Crt_Date = DateTime.Now;

            return await _repository.CreateAsync(entity);
        }

        public async Task<bool> DeactivateAsync(int serviceTypeId)
        {
            var existing = await _repository.GetByIdAsync(serviceTypeId)
                ?? throw new KeyNotFoundException($"Không tìm thấy ServiceType ID = {serviceTypeId}.");

            if (!existing.IsActive)
                throw new InvalidOperationException("Loại dịch vụ này đã bị vô hiệu hóa trước đó.");

            return await _repository.DeactivateAsync(serviceTypeId);
        }

        public async Task<IEnumerable<ServiceTypeDto>> GetAllActiveAsync()
        {
            var entities = await _repository.GetAllActiveAsync();
            return _mapper.Map<IEnumerable<ServiceTypeDto>>(entities);
        }

        public async Task<IEnumerable<ServiceTypeDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<ServiceTypeDto>>(entities);
        }

        public async Task<ServiceTypeDto?> GetByIdAsync(int serviceTypeId)
        {
            var entity = await _repository.GetByIdAsync(serviceTypeId);
            return entity is null ? null : _mapper.Map<ServiceTypeDto>(entity);
        }

        public async Task<bool> UpdateAsync(int serviceTypeId, UpdateServiceTypeDto dto, string chgeUser)
        {
            var existing = await _repository.GetByIdAsync(serviceTypeId)
                ?? throw new KeyNotFoundException($"Không tìm thấy ServiceType ID = {serviceTypeId}.");

            if (await _repository.IsCodeExistsAsync(dto.Code, excludeId: serviceTypeId))
                throw new InvalidOperationException($"Mã dịch vụ '{dto.Code.Trim().ToUpper()}' đã được sử dụng.");

            _mapper.Map(dto, existing);
            existing.ChgeUser = chgeUser;
            existing.ChgeDate = DateTime.Now;

            return await _repository.UpdateAsync(existing);
        }
               
    }
}
