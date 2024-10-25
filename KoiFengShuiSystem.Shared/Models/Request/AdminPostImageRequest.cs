using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class AdminPostImageRequest
    {
        public int PostId { get; set; }
        public int ImageId { get; set; }
        public string ImageDescription { get; set; }
    }
}