﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace KoiFengShuiSystem.DataAccess.Models;

public partial class Post
{
    public int PostId { get; set; }

    public int Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public double Price { get; set; }

    public DateTime CreateAt { get; set; }

    public DateTime UpdateAt { get; set; }

    public int AccountId { get; set; }

    public string Status { get; set; }

    public int ElementId { get; set; }

    public virtual Account Account { get; set; }

    public virtual Element Element { get; set; }

    public virtual ICollection<Follow> Follows { get; set; } = new List<Follow>();

    public virtual PostCategory IdNavigation { get; set; }

    public virtual ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();
}