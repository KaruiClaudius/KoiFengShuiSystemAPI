﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Response
{
    public class TransactionResponseDto
    {
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public int TierId { get; set; }
        public int SubscriptionId { get; set; }
        public decimal Amount { get; set; }
        public string MPLTitle { get; set; }
        public string Status { get; set; }
        public DateTime TransactionDate { get; set; }
    }
}