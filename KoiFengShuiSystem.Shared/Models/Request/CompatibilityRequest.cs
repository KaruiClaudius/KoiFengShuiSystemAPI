﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.Shared.Models.Request
{
    public class CompatibilityRequest
    {
        public int DateOfBirth { get; set; }
        public string Direction { get; set; }
        public string FishColor { get; set; }
        public int FishQuantity { get; set; }
        public string PondShape { get; set; }
    }
}