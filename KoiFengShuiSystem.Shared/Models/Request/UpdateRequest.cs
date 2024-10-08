﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class UpdateRequest
    {
        [EmailAddress]
        public string Email { get; set; }

        public string FullName { get; set; }
        public DateTime? Dob { get; set; }

        public string Gender { get; set; }
        public string Phone { get; set; }
    }
}
