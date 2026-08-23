using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Modules.FengShui.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.FengShui
{
    /// <summary>
    /// Regression pins for the consultation-engine efficiency rewrite: one reference
    /// snapshot per assessment, no redundant store traffic, and warm-cache assessments
    /// add zero load.
    /// </summary>
    public class FengShuiEfficiencyTests
    {
        private static List<Element> CreateElements()
        {
            var thuy = CungPhiCalculator.Calculate(1990, Gender.Male);
            return new List<Element>
            {
                new() { ElementId = 1, ElementName = thuy.Menh, LuckyNumber = "1,6" }
            };
        }

        private static List<Direction> CreateDirections() =>
            new()
            {
                new Direction { DirectionId = 1, DirectionName = "Dong" },
                new Direction { DirectionId = 2, DirectionName = "Tay" }
            };

        private static List<FengShuiDirection> CreateFsDirections(List<Element> elements)
        {
            var directions = CreateDirections();
            return new List<FengShuiDirection>
            {
                new() { Id = 1, DirectionId = 2, Direction = directions[1], ElementId = 1, Element = elements[0], Description = "Tot cho Thuy" }
            };
        }

        private static List<ShapeCategory> CreateShapes() =>
            new() { new ShapeCategory { ShapeId = 9, ShapeName = "Vuong", Description = "Hinh vuong", ElementId = 1 } };

        private static List<KoiBreed> CreateBreeds() =>
            new() { new KoiBreed { BreedId = 1, ElementId = 1, CountryId = 1, BreedName = "Kohaku", Color = "Den trang" } };

        /// <summary>
        /// Strict mock exposing ONLY the five reference methods the rewritten engine needs.
        /// If any legacy per-call lookup path resurfaces (GetElementByIdAsync,
        /// GetShapeByNameAndElementIdAsync, GetFengShuiDirectionsByElementIdAsync,
        /// GetDirectionByNameAsync during scoring), Moq fails with a missing-setup error.
        /// </summary>
        private static Mock<IFengShuiReadStore> CreateMinimalSnapshotMock()
        {
            var elements = CreateElements();
            var directions = CreateDirections();
            var fsDirections = CreateFsDirections(elements);
            var shapes = CreateShapes();
            var breeds = CreateBreeds();

            var store = new Mock<IFengShuiReadStore>(MockBehavior.Strict);
            store.Setup(s => s.GetElementByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((string name) => elements.FirstOrDefault(e => e.ElementName == name));
            store.Setup(s => s.GetAllDirectionsAsync()).ReturnsAsync(directions);
            store.Setup(s => s.GetAllFengShuiDirectionsWithDirectionAsync()).ReturnsAsync(fsDirections);
            store.Setup(s => s.GetAllShapeCategoriesAsync()).ReturnsAsync(shapes);
            store.Setup(s => s.GetAllKoiBreedsAsync()).ReturnsAsync(breeds);
            return store;
        }

        private static CompatibilityRequest CreateRequestNeedingAllRecommendations() =>
            new()
            {
                DateOfBirth = 1990,
                IsMale = true,
                Direction = "Dong",
                PondShape = "Tron",
                FishColors = new List<string> { "Xanh la" },
                FishQuantity = 3
            };

        [Fact]
        public async Task AssessCompatibility_UsesOnlySnapshotSurface_NoLegacyLookups()
        {
            var store = CreateMinimalSnapshotMock();
            var service = new CompatibilityService(store.Object, Mock.Of<ILogger<CompatibilityService>>());

            var result = await service.AssessCompatibility(CreateRequestNeedingAllRecommendations());

            Assert.NotNull(result);
            Assert.True(result.Recommendations.Count > 0, "Scenario must trigger recommendation branches.");
            store.Verify(s => s.GetAllKoiBreedsAsync(), Times.Once);
            store.Verify(s => s.GetAllShapeCategoriesAsync(), Times.Once);
            store.Verify(s => s.GetAllDirectionsAsync(), Times.Once);
            store.Verify(s => s.GetAllFengShuiDirectionsWithDirectionAsync(), Times.Once);
        }

        [Fact]
        public async Task AssessCompatibility_RecommendationText_UsesParsedQuantityNotExtraQueries()
        {
            var store = CreateMinimalSnapshotMock();
            var service = new CompatibilityService(store.Object, Mock.Of<ILogger<CompatibilityService>>());

            var result = await service.AssessCompatibility(CreateRequestNeedingAllRecommendations());

            var quantityAdvice = Assert.Single(result.Recommendations, r => r.Contains("Số lượng cá"));
            // LuckyNumber "1,6" → recommended last digit 6, derived from the element row
            // already in hand (legacy code re-queried the element twice per message).
            Assert.Contains("(6)", quantityAdvice);
            store.Verify(s => s.GetElementByIdAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SecondAssessment_ThroughCachingStore_AddsZeroStoreCalls()
        {
            var elements = CreateElements();
            var directions = CreateDirections();
            var fsDirections = CreateFsDirections(elements);
            var shapes = CreateShapes();
            var breeds = CreateBreeds();

            var inner = new Mock<IFengShuiReadStore>(MockBehavior.Strict);
            inner.Setup(s => s.GetElementByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((string name) => elements.FirstOrDefault(e => e.ElementName == name));
            inner.Setup(s => s.GetAllDirectionsAsync()).ReturnsAsync(directions);
            inner.Setup(s => s.GetAllFengShuiDirectionsWithDirectionAsync()).ReturnsAsync(fsDirections);
            inner.Setup(s => s.GetAllShapeCategoriesAsync()).ReturnsAsync(shapes);
            inner.Setup(s => s.GetAllKoiBreedsAsync()).ReturnsAsync(breeds);

            var cached = new CachedFengShuiReadStore(inner.Object, new MemoryCache(new MemoryCacheOptions()));
            var service = new CompatibilityService(cached, Mock.Of<ILogger<CompatibilityService>>());
            var request = CreateRequestNeedingAllRecommendations();

            await service.AssessCompatibility(request);
            await service.AssessCompatibility(request);

            inner.Verify(s => s.GetAllKoiBreedsAsync(), Times.Once);
            inner.Verify(s => s.GetAllShapeCategoriesAsync(), Times.Once);
            inner.Verify(s => s.GetElementByNameAsync(It.IsAny<string>()), Times.Once);
        }
    }
}
