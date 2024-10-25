using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
{
    public class AdminPostImageResponse
    {
        public int PostImageId { get; set; }
        public int PostId { get; set; }
        public int ImageId { get; set; }
        public string ImageDescription { get; set; }
        public ImageResponse Image { get; set; }
    }
}