using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.FengShui
{
    public class CompatibilityServiceTests
    {
        private static List<Element> CreateElements()
        {
            var thuyResult = CungPhiCalculator.Calculate(1990, Gender.Male);
            var mocResult = CungPhiCalculator.Calculate(1985, Gender.Male);
            var kimResult = CungPhiCalculator.Calculate(1984, Gender.Male);

            return new List<Element>
            {
                new Element { ElementId = 1, ElementName = thuyResult.Menh, Description = "Water", LuckyNumber = "1,6" },
                new Element { ElementId = 2, ElementName = mocResult.Menh, Description = "Wood", LuckyNumber = "3,8" },
                new Element { ElementId = 3, ElementName = "Ho\u1ea3", Description = "Fire", LuckyNumber = "2,7" },
                new Element { ElementId = 4, ElementName = "Th\u1ed5", Description = "Earth", LuckyNumber = "5,0" },
                new Element { ElementId = 5, ElementName = kimResult.Menh, Description = "Metal", LuckyNumber = "4,9" }
            };
        }

        private static List<Direction> CreateDirections()
        {
            return new List<Direction>
            {
                new Direction { DirectionId = 1, DirectionName = "Dong" },
                new Direction { DirectionId = 2, DirectionName = "Tay" },
                new Direction { DirectionId = 3, DirectionName = "Nam" },
                new Direction { DirectionId = 4, DirectionName = "Bac" }
            };
        }

        private static List<FengShuiDirection> CreateFengShuiDirections(List<Element> elements)
        {
            var directions = CreateDirections();
            return new List<FengShuiDirection>
            {
                new FengShuiDirection { Id = 1, DirectionId = 1, Direction = directions[0], ElementId = 1, Element = elements[0], Description = "Tot cho Thuy" },
                new FengShuiDirection { Id = 2, DirectionId = 2, Direction = directions[1], ElementId = 2, Element = elements[1], Description = "Tot cho Moc" }
            };
        }

        private static List<ShapeCategory> CreateShapeCategories()
        {
            return new List<ShapeCategory>
            {
                new ShapeCategory { ShapeId = 1, ShapeName = "Tron", Description = "Hinh tron", ElementId = 1 },
                new ShapeCategory { ShapeId = 2, ShapeName = "Vuong", Description = "Hinh vuong", ElementId = 2 },
                new ShapeCategory { ShapeId = 3, ShapeName = "Tam giac", Description = "Hinh tam giac", ElementId = null }
            };
        }

        private static List<KoiBreed> CreateKoiBreeds()
        {
            return new List<KoiBreed>
            {
                new KoiBreed { BreedId = 1, ElementId = 1, CountryId = 1, BreedName = "Kohaku", Color = "Do trang", Description = "Do va trang" },
                new KoiBreed { BreedId = 2, ElementId = 1, CountryId = 1, BreedName = "Taisho Sanke", Color = "Do den trang", Description = "Do den va trang" },
                new KoiBreed { BreedId = 3, ElementId = 2, CountryId = 1, BreedName = "Showa", Color = "Den do trang", Description = "Den do va trang" }
            };
        }

        private static Mock<IFengShuiReadStore> CreateSeededStoreMock()
        {
            var elements = CreateElements();
            var directions = CreateDirections();
            var fengShuiDirections = CreateFengShuiDirections(elements);
            var shapes = CreateShapeCategories();
            var breeds = CreateKoiBreeds();

            var storeMock = new Mock<IFengShuiReadStore>(MockBehavior.Strict);

            storeMock
                .Setup(s => s.GetElementByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((string name) => elements.FirstOrDefault(e => e.ElementName == name));

            storeMock
                .Setup(s => s.GetAllElementsAsync())
                .ReturnsAsync(elements);

            storeMock
                .Setup(s => s.GetDirectionByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((string name) => directions.FirstOrDefault(d => d.DirectionName == name));

            storeMock
                .Setup(s => s.GetFengShuiDirectionAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((int directionId, int elementId) =>
                    fengShuiDirections.FirstOrDefault(f => f.DirectionId == directionId && f.ElementId == elementId));

            storeMock
                .Setup(s => s.GetShapeByNameAndElementIdAsync(It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync((string shapeName, int elementId) =>
                    shapes.FirstOrDefault(s => s.ShapeName == shapeName && s.ElementId == elementId));

            storeMock
                .Setup(s => s.GetAllShapeCategoriesAsync())
                .ReturnsAsync(shapes);

            storeMock
                .Setup(s => s.GetAllKoiBreedsAsync())
                .ReturnsAsync(breeds);

            storeMock
                .Setup(s => s.GetAllFengShuiDirectionsWithDirectionAsync())
                .ReturnsAsync(fengShuiDirections);

            storeMock
                .Setup(s => s.GetElementByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int elementId) => elements.FirstOrDefault(e => e.ElementId == elementId));

            storeMock
                .Setup(s => s.GetFengShuiDirectionsByElementIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int elementId) => fengShuiDirections.Where(f => f.ElementId == elementId).ToList());

            storeMock
                .Setup(s => s.GetAllDirectionsAsync())
                .ReturnsAsync(directions);

            return storeMock;
        }

        private static CompatibilityService CreateService(Mock<IFengShuiReadStore> storeMock)
        {
            return new CompatibilityService(
                storeMock.Object,
                Mock.Of<ILogger<CompatibilityService>>());
        }

        private static CompatibilityRequest CreateValidRequest(int yearOfBirth = 1990)
        {
            return new CompatibilityRequest
            {
                DateOfBirth = yearOfBirth,
                IsMale = true,
                Direction = "Dong",
                PondShape = "Tron",
                FishColors = new List<string> { "Do trang" },
                FishQuantity = 6
            };
        }

        [Fact]
        public async Task AssessCompatibility_ValidRequest_ReturnsResponseWithScores()
        {
            var service = CreateService(CreateSeededStoreMock());
            var request = CreateValidRequest();

            var result = await service.AssessCompatibility(request);

            Assert.NotNull(result);
            Assert.InRange(result.OverallCompatibilityScore, 0, 100);
            Assert.Equal(100.0, result.DirectionScore);
            Assert.Equal(100.0, result.ShapeScore);
            Assert.Equal(100.0, result.QuantityScore);
            Assert.Contains("Do trang", result.ColorScores.Keys);
            Assert.Contains("TotalScore", result.ColorScores.Keys);
            Assert.Equal(100.0, result.ColorScores["TotalScore"]);
        }

        [Fact]
        public async Task AssessCompatibility_FullyCompatible_ReturnsPerfectOverallScore()
        {
            var service = CreateService(CreateSeededStoreMock());
            var request = CreateValidRequest();

            var result = await service.AssessCompatibility(request);

            Assert.Equal(100.0, result.OverallCompatibilityScore);
        }

        [Fact]
        public async Task AssessCompatibility_NoDirectionMatch_ReturnsZeroDirectionScore()
        {
            var service = CreateService(CreateSeededStoreMock());
            var request = CreateValidRequest();
            request.Direction = "NonExistent";

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.DirectionScore);
            Assert.False(result.OverallCompatibilityScore >= 100.0);
        }

        [Fact]
        public async Task AssessCompatibility_NoShapeMatch_ReturnsZeroShapeScore()
        {
            var service = CreateService(CreateSeededStoreMock());
            var request = CreateValidRequest();
            request.PondShape = "NonExistent";

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.ShapeScore);
        }

        [Fact]
        public async Task AssessCompatibility_NoQuantityMatch_ReturnsZeroQuantityScore()
        {
            var service = CreateService(CreateSeededStoreMock());
            var request = CreateValidRequest();
            request.FishQuantity = 5;

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.QuantityScore);
        }

        [Fact]
        public async Task AssessCompatibility_NoColorMatch_ReturnsZeroColorTotal()
        {
            var service = CreateService(CreateSeededStoreMock());
            var request = CreateValidRequest();
            request.FishColors = new List<string> { "Xanh" };

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.ColorScores["Xanh"]);
            Assert.Equal(0.0, result.ColorScores["TotalScore"]);
        }

        [Fact]
        public async Task AssessCompatibility_DifferentElement_ShapeAndDirectionNotMatch()
        {
            var service = CreateService(CreateSeededStoreMock());
            var request = CreateValidRequest(yearOfBirth: 1984);

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.DirectionScore);
            Assert.Equal(0.0, result.ShapeScore);
            Assert.Equal(0.0, result.QuantityScore);
        }

        [Fact]
        public async Task AssessCompatibility_InvalidYear_ThrowsException()
        {
            var service = CreateService(CreateSeededStoreMock());
            var request = CreateValidRequest(yearOfBirth: -1);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AssessCompatibility(request));
            Assert.Contains("date of birth", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AssessCompatibility_ElementNotInDatabase_ThrowsArgumentException()
        {
            var storeMock = new Mock<IFengShuiReadStore>(MockBehavior.Strict);
            storeMock
                .Setup(s => s.GetElementByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((Element?)null);

            var service = CreateService(storeMock);
            var request = CreateValidRequest();

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AssessCompatibility(request));
            Assert.Contains("element", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
