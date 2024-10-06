﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KoiFengShuiSystem.DataAccess.Models;

public partial class KoiFengShuiContext : DbContext
{
    public KoiFengShuiContext()
    {
    }

    public KoiFengShuiContext(DbContextOptions<KoiFengShuiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<CustomerFaP> CustomerFaPs { get; set; }

    public virtual DbSet<Direction> Directions { get; set; }

    public virtual DbSet<Element> Elements { get; set; }

    public virtual DbSet<FengShuiDirection> FengShuiDirections { get; set; }

    public virtual DbSet<FishPond> FishPonds { get; set; }

    public virtual DbSet<Follow> Follows { get; set; }

    public virtual DbSet<Image> Images { get; set; }

    public virtual DbSet<KoiBreed> KoiBreeds { get; set; }

    public virtual DbSet<MarketCategory> MarketCategories { get; set; }

    public virtual DbSet<MarketplaceListing> MarketplaceListings { get; set; }

    public virtual DbSet<Post> Posts { get; set; }

    public virtual DbSet<PostCategory> PostCategories { get; set; }

    public virtual DbSet<PostImage> PostImages { get; set; }

    public virtual DbSet<Recommendation> Recommendations { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<ShapeCategory> ShapeCategories { get; set; }

    public virtual DbSet<SubcriptionTier> SubcriptionTiers { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<TrafficLog> TrafficLogs { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public static string GetConnectionString(string connectionStringName)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        string connectionString = config.GetConnectionString(connectionStringName);
        return connectionString;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(GetConnectionString("DefaultConnection"));


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {

            entity.HasKey(e => e.AccountId).HasName("PK__Account__349DA5A66603EC7D");         

            entity.ToTable("Account");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Dob).HasColumnType("datetime");
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.FullName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Password).HasMaxLength(100);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Element).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.ElementId)
                .HasConstraintName("FK__Account__Element__4D94879B");

            entity.HasOne(d => d.Role).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.RoleId)
                .HasConstraintName("FK__Account__RoleId__4E88ABD4");
        });

        modelBuilder.Entity<Country>(entity =>
        {

            entity.HasKey(e => e.CountryId).HasName("PK__Country__10D1609F2A2F8FF3");


            entity.ToTable("Country");

            entity.Property(e => e.CountryName)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<CustomerFaP>(entity =>
        {

            entity.HasKey(e => e.FapId).HasName("PK__Customer__9D4BF20A50B4BDC8");


            entity.ToTable("CustomerFaP");

            entity.Property(e => e.Direction)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.DoB).HasColumnType("datetime");
            entity.Property(e => e.FishBreed)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(d => d.Element).WithMany(p => p.CustomerFaPs)
                .HasForeignKey(d => d.ElementId)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__CustomerF__Eleme__5CD6CB2B");

        });

        modelBuilder.Entity<Direction>(entity =>
        {

            entity.HasKey(e => e.DirectionId).HasName("PK__Directio__876847C67AD072F8");


            entity.ToTable("Direction");

            entity.Property(e => e.DirectionName)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<Element>(entity =>
        {

            entity.HasKey(e => e.ElementId).HasName("PK__Element__A429721A8BAB0039");


            entity.ToTable("Element");

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.ElementName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.LuckyNumber)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<FengShuiDirection>(entity =>
        {

            entity.HasKey(e => e.Id).HasName("PK__FengShui__3214EC078742B02C");


            entity.ToTable("FengShuiDirection");

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(d => d.Direction).WithMany(p => p.FengShuiDirections)
                .HasForeignKey(d => d.DirectionId)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__FengShuiD__Direc__3B75D760");


            entity.HasOne(d => d.Element).WithMany(p => p.FengShuiDirections)
                .HasForeignKey(d => d.ElementId)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__FengShuiD__Eleme__3C69FB99");

        });

        modelBuilder.Entity<FishPond>(entity =>
        {

            entity.HasKey(e => e.PondId).HasName("PK__FishPond__D18BF83453345957");


            entity.HasOne(d => d.DirectionPlacementNavigation).WithMany(p => p.FishPonds)
                .HasForeignKey(d => d.DirectionPlacement)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__FishPonds__Direc__403A8C7D");


            entity.HasOne(d => d.Shape).WithMany(p => p.FishPonds)
                .HasForeignKey(d => d.ShapeId)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__FishPonds__Shape__3F466844");

        });

        modelBuilder.Entity<Follow>(entity =>
        {

            entity.HasKey(e => e.FollowId).HasName("PK__Follow__2CE810AE3220E303");


            entity.ToTable("Follow");

            entity.HasOne(d => d.Account).WithMany(p => p.Follows)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__Follow__AccountI__33D4B598");


            entity.HasOne(d => d.Post).WithMany(p => p.Follows)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__Follow__PostId__32E0915F");

        });

        modelBuilder.Entity<Image>(entity =>
        {

            entity.HasKey(e => e.ImageId).HasName("PK__Image__7516F70C7A4A9140");


            entity.ToTable("Image");

            entity.Property(e => e.ImageUrl)
                .IsRequired()

                .HasMaxLength(255);

        });

        modelBuilder.Entity<KoiBreed>(entity =>
        {

            entity.HasKey(e => e.BreedId).HasName("PK__KoiBreed__D1E9AE9D7B6B7889");


            entity.Property(e => e.BreedName)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Color)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(d => d.Country).WithMany(p => p.KoiBreeds)
                .HasForeignKey(d => d.CountryId)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__KoiBreeds__Count__45F365D3");


            entity.HasOne(d => d.Element).WithMany(p => p.KoiBreeds)
                .HasForeignKey(d => d.ElementId)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__KoiBreeds__Eleme__44FF419A");

        });

        modelBuilder.Entity<MarketCategory>(entity =>
        {

            entity.HasKey(e => e.Categoryid).HasName("PK__MarketCa__190606233B34B8E2");


            entity.ToTable("MarketCategory");

            entity.Property(e => e.CategoryName)
                .IsRequired()
                .HasMaxLength(20);
        });

        modelBuilder.Entity<MarketplaceListing>(entity =>
        {

            entity.HasKey(e => e.ListingId).HasName("PK__Marketpl__BF3EBED09DDCFE45");


            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.ExpiresAt).HasColumnType("datetime");
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(d => d.Account).WithMany(p => p.MarketplaceListings)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)

                .HasConstraintName("FK__Marketpla__Accou__5441852A");


            entity.HasOne(d => d.Category).WithMany(p => p.MarketplaceListings)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Marketpla__Categ__534D60F1");


            entity.HasOne(d => d.Tier).WithMany(p => p.MarketplaceListings)
                .HasForeignKey(d => d.TierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Marketpla__TierI__5535A963");

        });

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PK__Post__AA12601810867FA2");


            entity.ToTable("Post");

            entity.Property(e => e.CreateAt).HasColumnType("datetime");
            entity.Property(e => e.Description)
                .IsRequired()
                .HasColumnType("text");
            entity.Property(e => e.Name)
                .IsRequired()
                .HasColumnType("text");
            entity.Property(e => e.UpdateAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.Posts)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Post__AccountId__2F10007B");


            entity.HasOne(d => d.Element).WithMany(p => p.Posts)
                .HasForeignKey(d => d.ElementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Post__ElementId__300424B4");


            entity.HasOne(d => d.IdNavigation).WithMany(p => p.Posts)
                .HasForeignKey(d => d.Id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Post__Id__534D60F1");
        });

        modelBuilder.Entity<PostCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PostCate__3214EC07FB6089BE");


            entity.ToTable("PostCategory");

            entity.Property(e => e.PostType)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<PostImage>(entity =>
        {
            entity.HasKey(e => e.PostImageId).HasName("PK__PostImag__BCD3CCD0F74FA821");


            entity.ToTable("PostImage");

            entity.HasOne(d => d.Image).WithMany(p => p.PostImages)
                .HasForeignKey(d => d.ImageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostImage__Image__628FA481");


            entity.HasOne(d => d.Post).WithMany(p => p.PostImages)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PostImage__PostI__619B8048");

        });

        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(e => e.RecommendationId).HasName("PK__Recommen__AA15BEE42C28CC98");

            entity.ToTable("Recommendation");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.Recommendations)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Recommend__Accou__48CFD27E");


            entity.HasOne(d => d.Breed).WithMany(p => p.Recommendations)
                .HasForeignKey(d => d.BreedId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Recommend__Breed__49C3F6B7");


            entity.HasOne(d => d.Pond).WithMany(p => p.Recommendations)
                .HasForeignKey(d => d.PondId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Recommend__PondI__4AB81AF0");

        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Role__8AFACE1AE5C490C7");


            entity.ToTable("Role");

            entity.Property(e => e.RoleName)
                .IsRequired()
                .HasMaxLength(50);
        });

        modelBuilder.Entity<ShapeCategory>(entity =>
        {
            entity.HasKey(e => e.ShapeId).HasName("PK__ShapeCat__70FC8381A01D0D30");


            entity.ToTable("ShapeCategory");

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.ShapeName)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasOne(d => d.Element).WithMany(p => p.ShapeCategories)
                .HasForeignKey(d => d.ElementId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ShapeCate__Eleme__36B12243");

        });

        modelBuilder.Entity<SubcriptionTier>(entity =>
        {
            entity.HasKey(e => e.TierId).HasName("PK__Subcript__362F561D0B860B35");


            entity.Property(e => e.TierName).HasMaxLength(1);
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__Subscrip__9A2B249D531FAD7A");


            entity.ToTable("Subscription");

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<TrafficLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TrafficL__3214EC07788AE368");

            entity.ToTable("TrafficLog");

            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.RequestMethod).HasMaxLength(10);
            entity.Property(e => e.RequestPath).HasMaxLength(255);
            entity.Property(e => e.Timestamp).HasColumnType("datetime");
            entity.Property(e => e.UserAgent).HasMaxLength(255);

            entity.HasOne(d => d.Account).WithMany(p => p.TrafficLogs)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_TrafficLog_Account");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransactionId).HasName("PK__Transact__55433A6B3CF372AD");


            entity.ToTable("Transaction");

            entity.Property(e => e.Amount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TransactionDate).HasColumnType("datetime");

            entity.HasOne(d => d.Account).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transacti__Accou__59063A47");


            entity.HasOne(d => d.Subscription).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.SubscriptionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transacti__Subsc__5812160E");


            entity.HasOne(d => d.Tier).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.TierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Transacti__TierI__59FA5E80");

        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}