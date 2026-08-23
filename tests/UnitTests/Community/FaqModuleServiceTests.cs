using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Services;
using Moq;

namespace UnitTests.Community
{
    public class FaqModuleServiceTests
    {
        private readonly Mock<ICommunityStore> _storeMock = new(MockBehavior.Strict);

        private FaqService CreateService() => new(_storeMock.Object);

        private static FAQRequest CreateValidRequest(int accountId = 1) => new()
        {
            Question = "How do I choose a koi for my pond?",
            Answer = "Match the koi's element to the owner's birth element.",
            AccountId = accountId
        };

        private static FAQ CreateEntity(int id, int accountId = 1) => new()
        {
            FAQId = id,
            Question = $"Question {id}",
            Answer = $"Answer {id}",
            CreateAt = new DateTime(2026, 1, 1, 0, 0, 0),
            AccountId = accountId
        };

        [Fact]
        public void Constructor_NullStore_ThrowsArgumentNullException()
        {
            var ex = Record.Exception(() => new FaqService(null!));

            Assert.NotNull(ex);
            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public async Task GetAllFAQsAsync_EmptyStore_ReturnsEmptyList()
        {
            _storeMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<FAQ>());

            var service = CreateService();

            var result = await service.GetAllFAQsAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllFAQsAsync_WithStoredFaqs_ReturnsAllMapped()
        {
            _storeMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<FAQ>
                {
                    CreateEntity(1),
                    CreateEntity(2)
                });

            var service = CreateService();

            var result = await service.GetAllFAQsAsync();

            Assert.Equal(2, result.Count);
            Assert.Equal("Question 1", result[0].Question);
            Assert.Equal("Answer 1", result[0].Answer);
            Assert.Equal(1, result[0].FAQId);
            Assert.Equal("Question 2", result[1].Question);
        }

        [Fact]
        public async Task GetFAQByIdAsync_NonExistentId_ReturnsNull()
        {
            _storeMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((FAQ?)null);

            var service = CreateService();

            var result = await service.GetFAQByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetFAQByIdAsync_ExistingId_ReturnsMappedResponse()
        {
            _storeMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(CreateEntity(1));

            var service = CreateService();

            var result = await service.GetFAQByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Question 1", result!.Question);
            Assert.Equal("Answer 1", result.Answer);
            Assert.Equal(CreateEntity(1).CreateAt, result.CreateAt);        }

        [Fact]
        public async Task CreateFAQAsync_ValidRequest_PersistsAndReturnsMappedResponse()
        {
            FAQ? captured = null;
            _storeMock
                .Setup(s => s.AddAsync(It.IsAny<FAQ>()))
                .Callback<FAQ>(faq => captured = faq)
                .ReturnsAsync((FAQ faq) =>
                {
                    faq.FAQId = 42;
                    return faq;
                });

            var service = CreateService();
            var request = CreateValidRequest(accountId: 7);

            var result = await service.CreateFAQAsync(request);

            Assert.NotNull(captured);
            Assert.Equal(request.Question, captured!.Question);
            Assert.Equal(request.Answer, captured.Answer);
            Assert.Equal(7, captured.AccountId);
            Assert.True(captured.CreateAt > DateTime.MinValue);

            Assert.Equal(42, result.FAQId);
            Assert.Equal(request.Question, result.Question);
            Assert.Equal(request.Answer, result.Answer);
        }

        [Fact]
        public async Task UpdateFAQAsync_NonExistentId_ReturnsNullAndSkipsWrite()
        {
            _storeMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((FAQ?)null);

            var service = CreateService();
            var request = CreateValidRequest();

            var result = await service.UpdateFAQAsync(999, request);

            Assert.Null(result);
            _storeMock.Verify(s => s.UpdateAsync(It.IsAny<FAQ>()), Times.Never);
        }

        [Fact]
        public async Task UpdateFAQAsync_ExistingId_RewritesContentAndTimestamp()
        {
            var stored = CreateEntity(1);
            var originalTimestamp = stored.CreateAt;
            FAQ? captured = null;
            _storeMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(stored);
            _storeMock
                .Setup(s => s.UpdateAsync(It.IsAny<FAQ>()))
                .Callback<FAQ>(faq => captured = faq)
                .Returns(Task.CompletedTask);

            var service = CreateService();
            var request = new FAQRequest
            {
                Question = "Updated question?",
                Answer = "Updated answer",
                AccountId = 99
            };

            var result = await service.UpdateFAQAsync(1, request);

            Assert.NotNull(captured);
            Assert.Same(stored, captured);
            Assert.Equal("Updated question?", captured!.Question);
            Assert.Equal("Updated answer", captured.Answer);
            Assert.NotEqual(originalTimestamp, captured.CreateAt);

            Assert.Equal("Updated question?", result!.Question);
            Assert.Equal("Updated answer", result.Answer);
        }

        [Fact]
        public async Task UpdateFAQAsync_ExistingId_PreservesAccountIdFromStore()
        {
            var stored = CreateEntity(1, accountId: 5);
            _storeMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(stored);
            _storeMock
                .Setup(s => s.UpdateAsync(It.IsAny<FAQ>()))
                .Returns(Task.CompletedTask);

            var service = CreateService();
            var request = new FAQRequest
            {
                Question = "Updated question?",
                Answer = "Updated answer",
                AccountId = 99
            };

            await service.UpdateFAQAsync(1, request);

            Assert.Equal(5, stored.AccountId);
        }

        [Fact]
        public async Task DeleteFAQAsync_NonExistentId_ReturnsFalse()
        {
            _storeMock
                .Setup(s => s.DeleteAsync(999))
                .ReturnsAsync(false);

            var service = CreateService();

            var result = await service.DeleteFAQAsync(999);

            Assert.False(result);
        }

        [Fact]
        public async Task DeleteFAQAsync_ExistingId_DelegatesToStoreAndReturnsTrue()
        {
            _storeMock
                .Setup(s => s.DeleteAsync(1))
                .ReturnsAsync(true);

            var service = CreateService();

            var result = await service.DeleteFAQAsync(1);

            Assert.True(result);
            _storeMock.Verify(s => s.DeleteAsync(1), Times.Once);
        }
    }
}
