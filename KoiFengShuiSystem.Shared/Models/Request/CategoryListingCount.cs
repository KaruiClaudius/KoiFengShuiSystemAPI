using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class CategoryListingCount
    {
        public string CategoryName { get; set; }
        public int Count { get; set; }
        public string CategoryOutput { get; set; }
    }
}
