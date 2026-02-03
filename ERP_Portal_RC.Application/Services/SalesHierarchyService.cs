using ERP_Portal_RC.Application.DTOs;
using ERP_Portal_RC.Application.Interfaces;
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
        public SalesHierarchyService(ISalesHierarchyRepository salesHierarchyRepository)
        {
            _salesHierarchyRepository = salesHierarchyRepository;
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
        private string GetImmediateParentId(string parentIDSortID)
        {
            if (string.IsNullOrEmpty(parentIDSortID)) return "";
            var parts = parentIDSortID.Split('.');
            return parts.Last();
        }
    }
}
