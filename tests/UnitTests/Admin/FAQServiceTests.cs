using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Admin
{
    public class FAQServiceTests
    {
        private static DbContextOptions<KoiFengShuiContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"FAQTestDb_{Guid.NewGuid()}")
                .Options;
        }

        private static KoiFengShuiContext CreateEmptyContext()
        {
            return new KoiFengShuiContext(CreateInMemoryOptions());
        }

        private static KoiFengShuiContext CreateContextWithSeedData()
        {
            var context = CreateEmptyContext();

            context.Accounts.Add(new Account
            {
                AccountId = 1,
                FullName = "Test User",
                Email = "test@test.com",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });

            context.FAQs.AddRange(
                new FAQ
                {
                    FAQId = 1,
                    Question = "Question 1",
                    Answer = "Answer 1",
                    CreateAt = DateTime.Now,
                    AccountId = 1
                },
                new FAQ
                {
                    FAQId = 2,
                    Question = "Question 2",
                    Answer = "Answer 2",
                    CreateAt = DateTime.Now,
                    AccountId = 1
                }
            );

            context.SaveChanges();
            return context;
        }

        [Fact]
        public void Constructor_AllowsNullContext()
        {
            var ex = Record.Exception(() => new FAQService(null!));
            Assert.Null(ex);
        }

        [Fact]
        public async Task GetAllFAQsAsync_EmptyDatabase_ReturnsEmptyList()
        {
            var service = new FAQService(CreateEmptyContext());

            var result = await service.GetAllFAQsAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllFAQsAsync_WithSeedData_ReturnsAllFAQs()
        {
            var context = CreateContextWithSeedData();
            var service = new FAQService(context);

            var result = await service.GetAllFAQsAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetFAQByIdAsync_NonExistentId_ReturnsNull()
        {
            var service = new FAQService(CreateEmptyContext());

            var result = await service.GetFAQByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFAQByIdAsync_ExistingId_ReturnsFAQ()
        {
            var context = CreateContextWithSeedData();
            var service = new FAQService(context);

            var result = await service.GetFAQByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Question 1", result.Question);
            Assert.Equal("Answer 1", result.Answer);
        }

        [Fact]
        public async Task CreateFAQAsync_ValidRequest_CreatesAndReturnsFAQ()
        {
            var context = CreateEmptyContext();
            context.Accounts.Add(new Account
            {
                AccountId = 1,
                FullName = "Test",
                Email = "test@test.com",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });
            context.SaveChanges();

            var service = new FAQService(context);
            var request = new FAQRequest
            {
                Question = "New Question?",
                Answer = "New Answer",
                AccountId = 1
            };

            var result = await service.CreateFAQAsync(request);

            Assert.NotNull(result);
            Assert.Equal("New Question?", result.Question);
            Assert.Equal("New Answer", result.Answer);
            Assert.True(result.CreateAt > DateTime.MinValue);
        }

        [Fact]
        public async Task UpdateFAQAsync_NonExistentId_ReturnsNull()
        {
            var service = new FAQService(CreateEmptyContext());
            var request = new FAQRequest
            {
                Question = "Updated?",
                Answer = "Updated Answer",
                AccountId = 1
            };

            var result = await service.UpdateFAQAsync(999, request);

            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateFAQAsync_ExistingId_UpdatesAndReturnsFAQ()
        {
            var context = CreateContextWithSeedData();
            var service = new FAQService(context);
            var request = new FAQRequest
            {
                Question = "Updated Question?",
                Answer = "Updated Answer",
                AccountId = 1
            };

            var result = await service.UpdateFAQAsync(1, request);

            Assert.NotNull(result);
            Assert.Equal("Updated Question?", result.Question);
            Assert.Equal("Updated Answer", result.Answer);
        }

        [Fact]
        public async Task DeleteFAQAsync_NonExistentId_ReturnsFalse()
        {
            var service = new FAQService(CreateEmptyContext());

            var result = await service.DeleteFAQAsync(999);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFAQAsync_ExistingId_DeletesAndReturnsTrue()
        {
            var context = CreateContextWithSeedData();
            var service = new FAQService(context);

            var result = await service.DeleteFAQAsync(1);

            Assert.True(result);
            Assert.Null(await context.FAQs.FindAsync(1));
        }
    }
}
