﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace KoiFengShuiSystem.DataAccess.Models;

public partial class Transaction
{
    public int Id { get; set; }

    public int TransactionId { get; set; }

    public int AccountId { get; set; }

    public int TierId { get; set; }

    public int SubscriptionId { get; set; }

    public decimal Amount { get; set; }

    public DateTime TransactionDate { get; set; }

    public string Status { get; set; }

    public int? ListingId { get; set; }

    public virtual Account Account { get; set; }

    public virtual MarketplaceListing Listing { get; set; }

    public virtual Subscription Subscription { get; set; }

    public virtual SubcriptionTier Tier { get; set; }
}