using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Responses;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Moq;

namespace UnitTests.FengShui
{
    public class PartnerShopServiceTests
    {
        private readonly Mock<IPartnerShopStore> _storeMock = new(MockBehavior.Strict);

        private PartnerShopService CreateService() => new(_storeMock.Object);

        private static PartnerShopRequest CreateValidRequest(bool isActive = true) => new()
        {
            Name = "Koi House Da Nang",
            Address = "123 Han River, Da Nang",
            LinkUrl = "https://koihouse.example.com",
            Note = "Wide selection of Kohaku",
            IsActive = isActive
        };

        private static PartnerShop CreateEntity(int id, bool isActive = true) => new()
        {
            Id = id,
            Name = $"Shop {id}",
            Address = $"Address {id}",
            LinkUrl = $"https://shop{id}.example.com",
            Note = null,
            IsActive = isActive,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        [Fact]
        public async Task CreateAsync_ValidRequest_PersistsAndReturnsMappedResponse()
        {
            PartnerShop? captured = null;
            _storeMock
                .Setup(s => s.AddAsync(It.IsAny<PartnerShop>()))
                .Callback<PartnerShop>(shop => captured = shop)
                .ReturnsAsync((PartnerShop shop) =>
                {
                    shop.Id = 42;
                    return shop;
                });

            var service = CreateService();
            var request = CreateValidRequest();

            var result = await service.CreateAsync(request);

            Assert.NotNull(captured);
            Assert.Equal("Koi House Da Nang", captured!.Name);
            Assert.Equal("123 Han River, Da Nang", captured.Address);
            Assert.Equal("https://koihouse.example.com", captured.LinkUrl);
            Assert.Equal("Wide selection of Kohaku", captured.Note);
            Assert.True(captured.IsActive);
            Assert.Equal(DateTimeKind.Utc, captured.CreatedAt.Kind);

            Assert.Equal(42, result.Id);
            Assert.Equal(request.Name, result.Name);
            Assert.Equal(request.Address, result.Address);
            Assert.Equal(request.LinkUrl, result.LinkUrl);
            Assert.Equal(request.Note, result.Note);
            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task CreateAsync_NullName_ThrowsArgumentException()
        {
            var service = CreateService();
            var request = CreateValidRequest();
            request.Name = null!;

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
            _storeMock.Verify(s => s.AddAsync(It.IsAny<PartnerShop>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_EmptyName_ThrowsArgumentException()
        {
            var service = CreateService();
            var request = CreateValidRequest();
            request.Name = "   ";

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
            _storeMock.Verify(s => s.AddAsync(It.IsAny<PartnerShop>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_EmptyLinkUrl_ThrowsArgumentException()
        {
            var service = CreateService();
            var request = CreateValidRequest();
            request.LinkUrl = string.Empty;

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
            _storeMock.Verify(s => s.AddAsync(It.IsAny<PartnerShop>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NonUrlLinkUrl_ThrowsArgumentException()
        {
            var service = CreateService();
            var request = CreateValidRequest();
            request.LinkUrl = "not-a-valid-url";

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
            _storeMock.Verify(s => s.AddAsync(It.IsAny<PartnerShop>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_FtpLinkUrl_ThrowsArgumentException()
        {
            var service = CreateService();
            var request = CreateValidRequest();
            request.LinkUrl = "ftp://files.example.com/koi";

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(request));
            _storeMock.Verify(s => s.AddAsync(It.IsAny<PartnerShop>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NullRequest_ThrowsArgumentNullException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.CreateAsync(null!));
        }

        [Fact]
        public async Task UpdateAsync_NullRequest_ThrowsArgumentNullException()
        {
            var service = CreateService();

            await Assert.ThrowsAsync<ArgumentNullException>(() => service.UpdateAsync(1, null!));
        }

        [Fact]
        public async Task UpdateAsync_DeactivatedShop_IsExcludedFromActiveList()
        {
            var shops = new List<PartnerShop> { CreateEntity(5, isActive: true) };
            _storeMock
                .Setup(s => s.GetByIdAsync(5))
                .ReturnsAsync(() => shops.Single(s => s.Id == 5));
            _storeMock
                .Setup(s => s.UpdateAsync(It.IsAny<PartnerShop>()))
                .Returns(Task.CompletedTask);
            _storeMock
                .Setup(s => s.GetActiveAsync())
                .ReturnsAsync(() => shops.Where(s => s.IsActive).ToList() as IReadOnlyList<PartnerShop>);

            var service = CreateService();
            var request = CreateValidRequest(isActive: false);

            await service.UpdateAsync(5, request);

            var active = await service.GetActiveAsync();

            Assert.DoesNotContain(active, response => response.Id == 5);
        }

        [Fact]
        public async Task GetActiveAsync_FiltersInactiveShopsAndMapsResponses()
        {
            var active = CreateEntity(1);
            _storeMock
                .Setup(s => s.GetActiveAsync())
                .ReturnsAsync(new List<PartnerShop> { active } as IReadOnlyList<PartnerShop>);

            var service = CreateService();

            var result = await service.GetActiveAsync();

            var response = Assert.Single(result);
            Assert.Equal(active.Id, response.Id);
            Assert.Equal(active.Name, response.Name);
            Assert.Equal(active.LinkUrl, response.LinkUrl);
            Assert.Equal(active.CreatedAt, response.CreatedAt);
            Assert.True(response.IsActive);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingShop_ReturnsMappedResponse()
        {
            var entity = CreateEntity(7);
            _storeMock
                .Setup(s => s.GetByIdAsync(7))
                .ReturnsAsync(entity);

            var service = CreateService();

            var result = await service.GetByIdAsync(7);

            Assert.Equal(7, result.Id);
            Assert.Equal(entity.Name, result.Name);
            Assert.Equal(entity.Note, result.Note);
            Assert.Equal(entity.IsActive, result.IsActive);
        }

        [Fact]
        public async Task GetByIdAsync_ShopNotFound_ThrowsKeyNotFoundException()
        {
            _storeMock
                .Setup(s => s.GetByIdAsync(999))
                .ReturnsAsync((PartnerShop?)null);

            var service = CreateService();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GetByIdAsync(999));
        }

        [Fact]
        public async Task UpdateAsync_ExistingShop_AppliesRequestValuesAndPersists()
        {
            var existing = CreateEntity(5, isActive: false);
            _storeMock
                .Setup(s => s.GetByIdAsync(5))
                .ReturnsAsync(existing);
            _storeMock
                .Setup(s => s.UpdateAsync(It.IsAny<PartnerShop>()))
                .Returns(Task.CompletedTask);

            var service = CreateService();
            var request = CreateValidRequest(isActive: true);
            request.Name = "Renamed Shop";
            request.LinkUrl = "https://renamed.example.com";

            await service.UpdateAsync(5, request);

            _storeMock.Verify(s => s.UpdateAsync(It.Is<PartnerShop>(shop =>
                shop.Id == 5 &&
                shop.Name == "Renamed Shop" &&
                shop.LinkUrl == "https://renamed.example.com" &&
                shop.IsActive)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ShopNotFound_ThrowsKeyNotFoundException()
        {
            _storeMock
                .Setup(s => s.GetByIdAsync(123))
                .ReturnsAsync((PartnerShop?)null);

            var service = CreateService();

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.UpdateAsync(123, CreateValidRequest()));
            _storeMock.Verify(s => s.UpdateAsync(It.IsAny<PartnerShop>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ExistingShop_ReturnsTrue()
        {
            _storeMock
                .Setup(s => s.DeleteAsync(9))
                .ReturnsAsync(true);

            var service = CreateService();

            Assert.True(await service.DeleteAsync(9));
        }

        [Fact]
        public async Task DeleteAsync_UnknownShop_ReturnsFalse()
        {
            _storeMock
                .Setup(s => s.DeleteAsync(404))
                .ReturnsAsync(false);

            var service = CreateService();

            Assert.False(await service.DeleteAsync(404));
        }
    }
}
