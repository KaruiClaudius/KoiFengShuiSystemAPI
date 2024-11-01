using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
{
    public class ListingImageResponse
    {
        public int ListingImageId { get; set; }
        public int ListingId { get; set; }
        public string ImageDescription { get; set; }
        public ImageResponse Image { get; set; }
    }
}
