using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class PostRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int AccountId { get; set; }
        public int? ElementId { get; set; }
        public string Status { get; set; }
        public List<PostImageRequest> Images { get; set; }
    }

    public class PostImageRequest
    {
        public string ImageUrl { get; set; }
        public string ImageDescription { get; set; }
    }
}
