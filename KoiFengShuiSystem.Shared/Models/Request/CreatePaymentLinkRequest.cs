using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class CreatePaymentLinkRequest
    {
        public string productName { get; set; }
        public string description { get; set; }
        public string returnUrl { get; set; }
        public string cancelUrl { get; set; }
        public int price { get; set; }
        public string buyerName { get; set; }
        public string buyerEmail { get; set; } // Add this line


    }
}
