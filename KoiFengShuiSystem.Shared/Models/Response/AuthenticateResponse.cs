using KoiFengShuiSystem.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
{
    public class AuthenticateResponse
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string Token { get; set; }


        public AuthenticateResponse(Account account, string token)
        {
            Id = account.AccountId;
            FullName = account.FullName;
            Email = account.Email;
            Token = token;
        }
    }
}
