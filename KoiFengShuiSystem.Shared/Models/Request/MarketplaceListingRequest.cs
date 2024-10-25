using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class MarketplaceListingRequest
    {
        public int? ListingId { get; set; }

        public int? AccountId { get; set; }

        public int TierId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public string Color { get; set; }

        public int Quantity { get; set; }

        public int CategoryId { get; set; }

        public DateTime? CreateAt { get; set; }

        public DateTime? ExpiresAt { get; set; }

        public bool IsActive { get; set; }

        public string? Status { get; set; }

        public int? ElementId { get; set; }
    }
}
