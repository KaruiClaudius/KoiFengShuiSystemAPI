using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Modules.FengShui.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace UnitTests.FengShui
{
    public class CachedFengShuiReadStoreTests
    {
        private static Element CreateElement(int id, string name) =>
            new() { ElementId = id, ElementName = name };

        [Fact]
        public async Task GetElementByNameAsync_SecondCall_ServedFromCache_UnderlyingHitOnce()
        {
            var inner = new Mock<IFengShuiReadStore>(MockBehavior.Strict);
            inner
                .Setup(s => s.GetElementByNameAsync("Thủy"))
                .ReturnsAsync(CreateElement(1, "Thủy"));

            var cached = new CachedFengShuiReadStore(inner.Object, new MemoryCache(new MemoryCacheOptions()));

            var first = await cached.GetElementByNameAsync("Thủy");
            var second = await cached.GetElementByNameAsync("Thủy");

            Assert.Equal(first, second);
            inner.Verify(s => s.GetElementByNameAsync("Thủy"), Times.Once);
        }

        [Fact]
        public async Task GetAllKoiBreedsAsync_Cached_AcrossConsumers()
        {
            var breeds = new List<KoiBreed> { new() { BreedId = 1, BreedName = "Kohaku" } };
            var inner = new Mock<IFengShuiReadStore>(MockBehavior.Strict);
            inner.Setup(s => s.GetAllKoiBreedsAsync()).ReturnsAsync(breeds);

            var cache = new MemoryCache(new MemoryCacheOptions());
            var consumerA = new CachedFengShuiReadStore(inner.Object, cache);
            var consumerB = new CachedFengShuiReadStore(inner.Object, cache);

            await consumerA.GetAllKoiBreedsAsync();
            await consumerB.GetAllKoiBreedsAsync();

            inner.Verify(s => s.GetAllKoiBreedsAsync(), Times.Once);
        }

        [Fact]
        public async Task DistinctArguments_CacheIndependently()
        {
            var inner = new Mock<IFengShuiReadStore>(MockBehavior.Strict);
            inner.Setup(s => s.GetElementByIdAsync(1)).ReturnsAsync(CreateElement(1, "Thủy"));
            inner.Setup(s => s.GetElementByIdAsync(2)).ReturnsAsync(CreateElement(2, "Mộc"));

            var cached = new CachedFengShuiReadStore(inner.Object, new MemoryCache(new MemoryCacheOptions()));

            var one = await cached.GetElementByIdAsync(1);
            var two = await cached.GetElementByIdAsync(2);
            var oneAgain = await cached.GetElementByIdAsync(1);

            Assert.Equal(1, one!.ElementId);
            Assert.Equal(2, two!.ElementId);
            Assert.Equal(one, oneAgain);
            inner.Verify(s => s.GetElementByIdAsync(It.IsAny<int>()), Times.Exactly(2));
        }

        [Fact]
        public async Task NullResult_IsCached_Too()
        {
            var inner = new Mock<IFengShuiReadStore>(MockBehavior.Strict);
            inner.Setup(s => s.GetDirectionByNameAsync("Nowhere")).ReturnsAsync((Direction?)null);

            var cached = new CachedFengShuiReadStore(inner.Object, new MemoryCache(new MemoryCacheOptions()));

            var first = await cached.GetDirectionByNameAsync("Nowhere");
            var second = await cached.GetDirectionByNameAsync("Nowhere");

            Assert.Null(first);
            Assert.Null(second);
            inner.Verify(s => s.GetDirectionByNameAsync("Nowhere"), Times.Once);
        }

        [Fact]
        public void Constructor_NullInner_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CachedFengShuiReadStore(null!, new MemoryCache(new MemoryCacheOptions())));
        }
    }
}
