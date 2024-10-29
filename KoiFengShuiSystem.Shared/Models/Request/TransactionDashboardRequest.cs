using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class TransactionDashboardRequest
    {
        public int TransactionId { get; set; }
        public string AccountFullName { get; set; }
        //public string TierName { get; set; }
        public string Status { get; set; }
        public DateTime TransactionDate { get; set; }
        public decimal Amount { get; set; }
    }
}
