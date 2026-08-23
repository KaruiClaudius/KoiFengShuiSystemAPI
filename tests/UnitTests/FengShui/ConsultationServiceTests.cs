using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Application.Services;
using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.FengShui
{
    public class ConsultationServiceTests
    {
        private static List<Element> CreateElements()
        {
            var thuyResult = CungPhiCalculator.Calculate(1990, Gender.Male);
            var mocResult = CungPhiCalculator.Calculate(1985, Gender.Male);
            var hoaResult = CungPhiCalculator.Calculate(2000, Gender.Male);
            var thoResult = CungPhiCalculator.Calculate(1995, Gender.Male);
            var kimResult = CungPhiCalculator.Calculate(1984, Gender.Male);

            return new List<Element>
            {
                new Element { ElementId = 1, ElementName = thuyResult.Menh, Description = "Water", LuckyNumber = "1,6" },
                new Element { ElementId = 2, ElementName = mocResult.Menh, Description = "Wood", LuckyNumber = "3,8" },
                new Element { ElementId = 3, ElementName = hoaResult.Menh, Description = "Fire", LuckyNumber = "2,7" },
                new Element { ElementId = 4, ElementName = thoResult.Menh, Description = "Earth", LuckyNumber = "5,0" },
                new Element { ElementId = 5, ElementName = kimResult.Menh, Description = "Metal", LuckyNumber = "4,9" }
            };
        }

        private static Mock<IFengShuiReadStore> CreateSeededStoreMock()
        {
            var elements = CreateElements();
            var directions = new List<Direction>
            {
                new Direction { DirectionId = 1, DirectionName = "Dong" },
                new Direction { DirectionId = 2, DirectionName = "Tay" }
            };

            var shapes = new List<ShapeCategory>
            {
                new ShapeCategory { ShapeId = 1, ShapeName = "Tron", Description = "Hinh tron", ElementId = 1 },
                new ShapeCategory { ShapeId = 2, ShapeName = "Vuong", Description = "Hinh vuong", ElementId = null }
            };

            var breeds = new List<KoiBreed>
            {
                new KoiBreed { BreedId = 1, ElementId = 1, CountryId = 1, BreedName = "Kohaku", Color = "Do trang", Description = "Do va trang" },
                new KoiBreed { BreedId = 2, ElementId = 2, CountryId = 1, BreedName = "Showa", Color = "Den do trang", Description = "Den do va trang" }
            };

            var fengShuiDirections = new List<FengShuiDirection>
            {
                new FengShuiDirection { Id = 1, DirectionId = 1, Direction = directions[0], ElementId = 1, Element = elements[0], Description = "Tot cho Thuy" }
            };

            var storeMock = new Mock<IFengShuiReadStore>(MockBehavior.Strict);

            storeMock
                .Setup(s => s.GetElementByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((string name) => elements.FirstOrDefault(e => e.ElementName == name));

            storeMock
                .Setup(s => s.GetAllElementsAsync())
                .ReturnsAsync(elements);

            storeMock
                .Setup(s => s.GetAllShapeCategoriesAsync())
                .ReturnsAsync(shapes);

            storeMock
                .Setup(s => s.GetAllKoiBreedsAsync())
                .ReturnsAsync(breeds);

            storeMock
                .Setup(s => s.GetAllFengShuiDirectionsWithDirectionAsync())
                .ReturnsAsync(fengShuiDirections);

            return storeMock;
        }

        private static ConsultationService CreateService(Mock<IFengShuiReadStore> storeMock)
        {
            return new ConsultationService(
                storeMock.Object,
                Mock.Of<ILogger<ConsultationService>>());
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidBirthYear_ReturnsResponseWithElement()
        {
            var service = CreateService(CreateSeededStoreMock());

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result);
            var expected = CungPhiCalculator.Calculate(1990, Gender.Male);
            Assert.Equal(expected.Menh, result.Element);
            Assert.Equal(expected.Cung, result.Cung);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidRequest_ReturnsLuckyNumbers()
        {
            var service = CreateService(CreateSeededStoreMock());

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result.LuckyNumbers);
            Assert.Contains("1", result.LuckyNumbers);
            Assert.Contains("6", result.LuckyNumbers);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidRequest_ReturnsMatchingFishBreeds()
        {
            var service = CreateService(CreateSeededStoreMock());

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result.FishBreeds);
            Assert.Contains("Kohaku", result.FishBreeds);
            Assert.DoesNotContain("Showa", result.FishBreeds);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidRequest_ReturnsFishColors()
        {
            var service = CreateService(CreateSeededStoreMock());

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result.FishColors);
            Assert.Contains("Do trang", result.FishColors);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidRequest_ReturnsPondShapeRecommendations()
        {
            var service = CreateService(CreateSeededStoreMock());

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result.SuggestedPonds);
            var recommended = result.SuggestedPonds.Where(s => s.IsRecommended).ToList();
            var notRecommended = result.SuggestedPonds.Where(s => !s.IsRecommended).ToList();
            Assert.Contains(recommended, r => r.ShapeName == "Tron");
            Assert.Contains(notRecommended, r => r.ShapeName == "Vuong");
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidRequest_ReturnsDirectionRecommendations()
        {
            var service = CreateService(CreateSeededStoreMock());

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result.SuggestedDirections);
            Assert.Contains(result.SuggestedDirections, d => d.DirectionName == "Dong" && d.IsRecommended);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_MaleVsFemale_ReturnsDifferentElements()
        {
            var service = CreateService(CreateSeededStoreMock());

            var maleResult = await service.GetFengShuiConsultationAsync(1990, true);
            var femaleResult = await service.GetFengShuiConsultationAsync(1990, false);

            Assert.NotEqual(maleResult.Element, femaleResult.Element);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ElementNotInDatabase_ThrowsApplicationException()
        {
            var storeMock = new Mock<IFengShuiReadStore>(MockBehavior.Strict);
            storeMock
                .Setup(s => s.GetElementByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((Element?)null);

            var service = CreateService(storeMock);

            var ex = await Assert.ThrowsAsync<ApplicationException>(() => service.GetFengShuiConsultationAsync(1990, true));
            Assert.Contains("Error processing", ex.Message);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_InvalidYear_ThrowsApplicationException()
        {
            var service = CreateService(CreateSeededStoreMock());

            var ex = await Assert.ThrowsAsync<ApplicationException>(() => service.GetFengShuiConsultationAsync(-1, true));
            Assert.Contains("Error processing", ex.Message);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_FemaleYear2000_ReturnsCorrectElement()
        {
            var service = CreateService(CreateSeededStoreMock());

            var result = await service.GetFengShuiConsultationAsync(2000, false);

            Assert.NotNull(result);
            Assert.Equal("Kim", result.Element);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_MaleYear2000_ReturnsCorrectElement()
        {
            var service = CreateService(CreateSeededStoreMock());

            var result = await service.GetFengShuiConsultationAsync(2000, true);

            Assert.NotNull(result);
            var expected = CungPhiCalculator.Calculate(2000, Gender.Male);
            Assert.Equal(expected.Menh, result.Element);
        }
    }
}
