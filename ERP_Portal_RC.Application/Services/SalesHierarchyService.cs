using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
using ERP_Portal_RC.Domain.Entities;
using ERP_Portal_RC.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Application.Services
{
    public class SalesHierarchyService : ISalesHierarchyService
    {
        private readonly ISalesHierarchyRepository _salesHierarchyRepository;
        private readonly ICustomStore _customStore;
        public SalesHierarchyService(ISalesHierarchyRepository salesHierarchyRepository, ICustomStore customStore)
        {
            _salesHierarchyRepository = salesHierarchyRepository;
            _customStore = customStore;
        }
        public async Task<List<ManagerDto>> GetManagerTreeAsync(string clnID, bool isManager)
        {
            var rawData = await _salesHierarchyRepository.GetRawSalesTreeAsync(clnID);

            var filteredData = isManager
                ? rawData.Where(x => x.IsGroup)
                : rawData;
            var processedList = filteredData
                .GroupBy(x => x.ItemID)
                .Select(g => g.First())
                .ToList();
            var map = processedList.ToDictionary(x => x.ItemID, x => new ManagerDto
            {
                Id = x.ItemID,
                Name = x.ItemName,
                Level = x.LEVEL_VAL,
                SortID = x.SortID,
                IsGroup = x.IsGroup 
            });

            var tree = new List<ManagerDto>();

            foreach (var item in processedList)
            {
                var dto = map[item.ItemID];

                string parentId = GetImmediateParentId(item.ParentIDSortID);

                if (!string.IsNullOrEmpty(parentId) && map.ContainsKey(parentId))
                {
                    map[parentId].Children.Add(dto);
                }
                else if (string.IsNullOrEmpty(item.PARENTID) || item.PARENTID == "*****")
                {
                    tree.Add(dto);
                }
                else if (!map.ContainsKey(parentId))
                {
                    tree.Add(dto);
                }
            }
            return tree;
        }

        public async Task<RegistrationResultDto> HandleSaleRegistrationAsync(SaleRegistrationModel request)
        {
            if (string.IsNullOrEmpty(request.FullName) || string.IsNullOrEmpty(request.Email))
            {
                throw new ArgumentException("FullName, Email are required fields.");
            }

            if (request.IsCreateAccount)
            {
                if (request.LoginName?.Length < 5) throw new Exception("Tên đăng nhập phải từ 5 ký tự.");
                if (request.Password?.Length < 6) throw new Exception("Mật khẩu phải từ 6 ký tự.");

                int isExisted = await _customStore.ChkUser(request.LoginName!);
                if (isExisted > 0) throw new Exception("Tên đăng nhập này đã tồn tại.");
            }

            string newEmplId = await _salesHierarchyRepository.RegisterSaleHierarchyAsync(request, "000642");

            string? newUserCode = null;


            // Tạo tk ERP nếu tick sử dụng
            if (request.IsCreateAccount)
            {
                newUserCode = await _salesHierarchyRepository.CreateERPAccountOnlyAsync(
                    request.LoginName!,
                    request.Password!,
                    request.FullName!,
                    request.Email!,
                    newEmplId);

                await _customStore.AddUserToGroup(newUserCode);
            }

            return new RegistrationResultDto
            {
                NewEmployeeID = newEmplId,
                NewUserCode = newUserCode
            };
        }

        private string GetImmediateParentId(string parentIDSortID)
        {
            if (string.IsNullOrEmpty(parentIDSortID)) return "";
            var parts = parentIDSortID.Split('.');
            return parts.Last();
        }
    }
}
