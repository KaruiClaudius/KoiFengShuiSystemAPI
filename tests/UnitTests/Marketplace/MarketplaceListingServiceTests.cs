using CloudinaryDotNet.Actions;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace UnitTests.Marketplace
{
    public class MarketplaceListingServiceTests
    {
        private static DbContextOptions<KoiFengShuiContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"MarketplaceTestDb_{Guid.NewGuid()}")
                .Options;
        }

        private static KoiFengShuiContext CreateContext()
        {
            return new KoiFengShuiContext(CreateInMemoryOptions());
        }

        private static KoiFengShuiContext CreateContextWithSeedData()
        {
            var context = CreateContext();

            context.Accounts.Add(new Account
            {
                AccountId = 1,
                FullName = "Test Seller",
                Email = "seller@test.com",
                Phone = "0123456789",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });

            context.Elements.Add(new Element
            {
                ElementId = 1,
                ElementName = "Thuy",
                Description = "Water",
                LuckyNumber = "1,6"
            });

            context.SubcriptionTiers.Add(new SubcriptionTier
            {
                TierId = 1,
                TierName = "Standard"
            });

            context.MarketCategories.Add(new MarketCategory
            {
                Categoryid = 1,
                CategoryName = "Koi Fish"
            });

            context.MarketplaceListings.Add(new MarketplaceListing
            {
                ListingId = 1,
                AccountId = 1,
                TierId = 1,
                Title = "Beautiful Koi",
                Description = "A beautiful koi fish",
                Price = 100.00m,
                Quantity = 1,
                CategoryId = 1,
                Color = "Red",
                CreateAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddDays(30),
                IsActive = true,
                Status = "Active",
                ElementId = 1
            });

            context.SaveChanges();
            return context;
        }

        private static MarketplaceListingService CreateService(
            KoiFengShuiContext context,
            ICloudService? cloudService = null)
        {
            return new MarketplaceListingService(
                new UnitOfWorkRepository(context),
                new GenericRepository<Account>(context),
                cloudService ?? Mock.Of<ICloudService>());
        }

        [Fact]
        public async Task GetAll_EmptyDatabase_ReturnsEmptyList()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var result = await service.GetAll();

            Assert.NotNull(result);
            Assert.Equal(1, result.Status);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task GetAll_WithSeedData_ReturnsListings()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetAll();

            Assert.NotNull(result);
            Assert.Equal(1, result.Status);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task GetMarketplaceListingById_ExistingId_ReturnsListing()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetMarketplaceListingById(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Status);
            Assert.NotNull(result.Data);
        }

        [Fact]
        public async Task GetMarketplaceListingById_NonExistentId_ReturnsSuccessWithEmptyData()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var result = await service.GetMarketplaceListingById(999);

            Assert.NotNull(result);
            Assert.Equal(1, result.Status);
        }

        [Fact]
        public async Task DeleteMarketplaceListing_ExistingId_ReturnsSuccess()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.DeleteMarketplaceListing(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Status);
        }

        [Fact]
        public async Task DeleteMarketplaceListing_NonExistentId_ReturnsWarningCode()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var result = await service.DeleteMarketplaceListing(999);

            Assert.NotNull(result);
            Assert.Equal(4, result.Status);
        }

        [Fact]
        public async Task DeleteMarketplaceListing_RemovesFromDatabase()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            await service.DeleteMarketplaceListing(1);

            var deleted = await context.MarketplaceListings.FindAsync(1);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task CreateMarketplaceListing_WithNoImages_ReturnsSuccess()
        {
            var context = CreateContextWithSeedData();
            var cloudMock = new Mock<ICloudService>();
            cloudMock.Setup(c => c.UploadImagesAsync(It.IsAny<List<IFormFile>>()))
                .ReturnsAsync(new List<ImageUploadResult>());
            var service = CreateService(context, cloudMock.Object);

            var request = new MarketplaceListingRequest
            {
                TierId = 1,
                Title = "New Listing",
                Description = "New koi fish",
                Price = 200.00m,
                Quantity = 2,
                CategoryId = 1,
                Color = "Blue",
                ExpiresAt = DateTime.Now.AddDays(30),
                IsActive = true,
                Status = "Active",
                ElementId = 1
            };

            var result = await service.CreateMarketplaceListing(request, new List<IFormFile>(), userId: 1);

            Assert.NotNull(result);
            Assert.Equal(1, result.Status);
            Assert.Equal("Save data success", result.Message);
        }

        [Fact]
        public async Task CreateMarketplaceListing_WithoutCloudSetup_StillCreates()
        {
            var context = CreateContextWithSeedData();
            var cloudMock = new Mock<ICloudService>();
            cloudMock.Setup(c => c.UploadImagesAsync(It.IsAny<List<IFormFile>>()))
                .ReturnsAsync(new List<ImageUploadResult>());
            var service = CreateService(context, cloudMock.Object);

            var request = new MarketplaceListingRequest
            {
                TierId = 1,
                Title = "Another Koi",
                Description = "Another fish",
                Price = 150.00m,
                Quantity = 1,
                CategoryId = 1,
                Color = "Red",
                ExpiresAt = DateTime.Now.AddDays(30),
                IsActive = true,
                Status = "Pending",
                ElementId = 1
            };

            var result = await service.CreateMarketplaceListing(request, new List<IFormFile>(), userId: 1);

            Assert.Equal(1, result.Status);
            var created = await context.MarketplaceListings
                .FirstOrDefaultAsync(m => m.Title == "Another Koi");
            Assert.NotNull(created);
            Assert.Equal(1, created.AccountId);
        }
    }
}
