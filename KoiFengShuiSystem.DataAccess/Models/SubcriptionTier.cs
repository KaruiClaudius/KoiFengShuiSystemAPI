﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace KoiFengShuiSystem.DataAccess.Models;

public partial class SubcriptionTier
{
    public int TierId { get; set; }

    public string TierName { get; set; }

    public virtual ICollection<MarketplaceListing> MarketplaceListings { get; set; } = new List<MarketplaceListing>();

    public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}