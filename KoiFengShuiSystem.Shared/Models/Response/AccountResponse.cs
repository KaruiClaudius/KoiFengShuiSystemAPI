using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
{
    public class AccountResponse
    {
        public int AccountId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime Dob { get; set; }
        public string Phone { get; set; }
        public string Gender { get; set; }
        public int? RoleId { get; set; }
        public string ElementName { get; set; }

    }
}
