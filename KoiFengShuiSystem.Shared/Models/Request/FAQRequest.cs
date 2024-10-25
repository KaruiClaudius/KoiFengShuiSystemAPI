using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class FAQRequest
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public int AccountId { get; set; } 

    }
}