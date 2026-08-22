using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.FengShui
{
    public class ConsultationServiceTests
    {
        private static DbContextOptions<KoiFengShuiContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"ConsultationTestDb_{Guid.NewGuid()}")
                .Options;
        }

        private static KoiFengShuiContext CreateContextWithSeedData()
        {
            var context = new KoiFengShuiContext(CreateInMemoryOptions());

            var thuyResult = CungPhiCalculator.Calculate(1990, Gender.Male);
            var mocResult = CungPhiCalculator.Calculate(1985, Gender.Male);
            var hoaResult = CungPhiCalculator.Calculate(2000, Gender.Male);
            var thoResult = CungPhiCalculator.Calculate(1995, Gender.Male);
            var kimResult = CungPhiCalculator.Calculate(1984, Gender.Male);

            context.Elements.AddRange(
                new Element { ElementId = 1, ElementName = thuyResult.Menh, Description = "Water", LuckyNumber = "1,6" },
                new Element { ElementId = 2, ElementName = mocResult.Menh, Description = "Wood", LuckyNumber = "3,8" },
                new Element { ElementId = 3, ElementName = hoaResult.Menh, Description = "Fire", LuckyNumber = "2,7" },
                new Element { ElementId = 4, ElementName = thoResult.Menh, Description = "Earth", LuckyNumber = "5,0" },
                new Element { ElementId = 5, ElementName = kimResult.Menh, Description = "Metal", LuckyNumber = "4,9" }
            );

            context.ShapeCategories.AddRange(
                new ShapeCategory { ShapeId = 1, ShapeName = "Tron", Description = "Hinh tron", ElementId = 1 },
                new ShapeCategory { ShapeId = 2, ShapeName = "Vuong", Description = "Hinh vuong", ElementId = null }
            );

            context.KoiBreeds.AddRange(
                new KoiBreed { BreedId = 1, ElementId = 1, CountryId = 1, BreedName = "Kohaku", Color = "Do trang", Description = "Do va trang" },
                new KoiBreed { BreedId = 2, ElementId = 2, CountryId = 1, BreedName = "Showa", Color = "Den do trang", Description = "Den do va trang" }
            );

            context.Directions.AddRange(
                new Direction { DirectionId = 1, DirectionName = "Dong" },
                new Direction { DirectionId = 2, DirectionName = "Tay" }
            );

            context.FengShuiDirections.AddRange(
                new FengShuiDirection { Id = 1, DirectionId = 1, ElementId = 1, Description = "Tot cho Thuy" }
            );

            context.SaveChanges();
            return context;
        }

        private static ConsultationService CreateService(KoiFengShuiContext context)
        {
            return new ConsultationService(
                new GenericRepository<Element>(context),
                new GenericRepository<KoiBreed>(context),
                new GenericRepository<ShapeCategory>(context),
                new GenericRepository<FengShuiDirection>(context),
                Mock.Of<ILogger<ConsultationService>>());
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidBirthYear_ReturnsResponseWithElement()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result);
            var expected = CungPhiCalculator.Calculate(1990, Gender.Male);
            Assert.Equal(expected.Menh, result.Element);
            Assert.Equal(expected.Cung, result.Cung);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidRequest_ReturnsLuckyNumbers()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result.LuckyNumbers);
            Assert.Contains("1", result.LuckyNumbers);
            Assert.Contains("6", result.LuckyNumbers);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidRequest_ReturnsMatchingFishBreeds()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result.FishBreeds);
            Assert.Contains("Kohaku", result.FishBreeds);
            Assert.DoesNotContain("Showa", result.FishBreeds);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidRequest_ReturnsFishColors()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result.FishColors);
            Assert.Contains("Do trang", result.FishColors);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ValidRequest_ReturnsPondShapeRecommendations()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

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
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetFengShuiConsultationAsync(1990, true);

            Assert.NotNull(result.SuggestedDirections);
            Assert.Contains(result.SuggestedDirections, d => d.DirectionName == "Dong" && d.IsRecommended);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_MaleVsFemale_ReturnsDifferentElements()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var maleResult = await service.GetFengShuiConsultationAsync(1990, true);
            var femaleResult = await service.GetFengShuiConsultationAsync(1990, false);

            Assert.NotEqual(maleResult.Element, femaleResult.Element);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_ElementNotInDatabase_ThrowsApplicationException()
        {
            var options = CreateInMemoryOptions();
            var context = new KoiFengShuiContext(options);
            context.SaveChanges();
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<ApplicationException>(() => service.GetFengShuiConsultationAsync(1990, true));
            Assert.Contains("Error processing", ex.Message);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_InvalidYear_ThrowsApplicationException()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<ApplicationException>(() => service.GetFengShuiConsultationAsync(-1, true));
            Assert.Contains("Error processing", ex.Message);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_FemaleYear2000_ReturnsCorrectElement()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetFengShuiConsultationAsync(2000, false);

            Assert.NotNull(result);
            Assert.Equal("Kim", result.Element);
        }

        [Fact]
        public async Task GetFengShuiConsultationAsync_MaleYear2000_ReturnsCorrectElement()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetFengShuiConsultationAsync(2000, true);

            Assert.NotNull(result);
            var expected = CungPhiCalculator.Calculate(2000, Gender.Male);
            Assert.Equal(expected.Menh, result.Element);
        }
    }
}
