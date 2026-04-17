using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_Portal_RC.Domain.Common
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int Total { get; set; }
        public int Page { get; set; } = 1;
        public int Size { get; set; } = 20;
        public int TotalPages => Size > 0 ? (int)Math.Ceiling(Total / (double)Size) : 0;
        public bool HasNext => Page < TotalPages;
        public bool HasPrevious => Page > 1;

        public PagedResult() { }

        public PagedResult(IEnumerable<T> items, int total, int page, int size)
        {
            Items = items;
            Total = total;
            Page = page;
            Size = size;
        }
    }
}
