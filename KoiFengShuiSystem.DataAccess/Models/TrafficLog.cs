﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace KoiFengShuiSystem.DataAccess.Models;

public partial class TrafficLog
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    public bool IsRegistered { get; set; }

    public int? AccountId { get; set; }

    public string IpAddress { get; set; }

    public string UserAgent { get; set; }

    public string RequestPath { get; set; }

    public string RequestMethod { get; set; }

    public virtual Account Account { get; set; }
}