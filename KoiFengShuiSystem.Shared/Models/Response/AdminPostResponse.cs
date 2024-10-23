using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
{
    public class AdminPostResponse
    {
        public int PostId { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public int AccountId { get; set; }
        public int? ElementId { get; set; }
        public string Status { get; set; }
        public string ElementName { get; set; }
        public string AccountName { get; set; }
    }
}
