using System.Collections.Generic;

namespace Core.Models
{
    public class PageResult<T>
         where T : class
    {
        public PageResult()
        {
            Items = new List<T>();
        }

        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
    }
}