using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.Common.FengShui;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.FengShui
{
    public class CompatibilityServiceTests
    {
        private static DbContextOptions<KoiFengShuiContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"CompatibilityTestDb_{Guid.NewGuid()}")
                .Options;
        }

        private static KoiFengShuiContext CreateContextWithSeedData()
        {
            var context = new KoiFengShuiContext(CreateInMemoryOptions());

            var thuyResult = CungPhiCalculator.Calculate(1990, true);
            var mocResult = CungPhiCalculator.Calculate(1985, true);
            var kimResult = CungPhiCalculator.Calculate(1984, true);

            context.Elements.AddRange(
                new Element { ElementId = 1, ElementName = thuyResult.Menh, Description = "Water", LuckyNumber = "1,6" },
                new Element { ElementId = 2, ElementName = mocResult.Menh, Description = "Wood", LuckyNumber = "3,8" },
                new Element { ElementId = 3, ElementName = "Ho\u1ea3", Description = "Fire", LuckyNumber = "2,7" },
                new Element { ElementId = 4, ElementName = "Th\u1ed5", Description = "Earth", LuckyNumber = "5,0" },
                new Element { ElementId = 5, ElementName = kimResult.Menh, Description = "Metal", LuckyNumber = "4,9" }
            );

            context.Directions.AddRange(
                new Direction { DirectionId = 1, DirectionName = "Dong" },
                new Direction { DirectionId = 2, DirectionName = "Tay" },
                new Direction { DirectionId = 3, DirectionName = "Nam" },
                new Direction { DirectionId = 4, DirectionName = "Bac" }
            );

            context.FengShuiDirections.AddRange(
                new FengShuiDirection { Id = 1, DirectionId = 1, ElementId = 1, Description = "Tot cho Thuy" },
                new FengShuiDirection { Id = 2, DirectionId = 2, ElementId = 2, Description = "Tot cho Moc" }
            );

            context.ShapeCategories.AddRange(
                new ShapeCategory { ShapeId = 1, ShapeName = "Tron", Description = "Hinh tron", ElementId = 1 },
                new ShapeCategory { ShapeId = 2, ShapeName = "Vuong", Description = "Hinh vuong", ElementId = 2 },
                new ShapeCategory { ShapeId = 3, ShapeName = "Tam giac", Description = "Hinh tam giac", ElementId = null }
            );

            context.KoiBreeds.AddRange(
                new KoiBreed { BreedId = 1, ElementId = 1, CountryId = 1, BreedName = "Kohaku", Color = "Do trang", Description = "Do va trang" },
                new KoiBreed { BreedId = 2, ElementId = 1, CountryId = 1, BreedName = "Taisho Sanke", Color = "Do den trang", Description = "Do den va trang" },
                new KoiBreed { BreedId = 3, ElementId = 2, CountryId = 1, BreedName = "Showa", Color = "Den do trang", Description = "Den do va trang" }
            );

            context.SaveChanges();
            return context;
        }

        private static CompatibilityService CreateService(KoiFengShuiContext context)
        {
            return new CompatibilityService(
                new GenericRepository<Element>(context),
                new GenericRepository<FengShuiDirection>(context),
                new GenericRepository<ShapeCategory>(context),
                new GenericRepository<KoiBreed>(context),
                new GenericRepository<Direction>(context),
                new GenericRepository<Recommendation>(context),
                new GenericRepository<FishPond>(context),
                Mock.Of<ILogger<CompatibilityService>>());
        }

        [Fact]
        public async Task AssessCompatibility_ValidRequest_ReturnsResponseWithScores()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var request = new CompatibilityRequest
            {
                DateOfBirth = 1990,
                IsMale = true,
                Direction = "Dong",
                PondShape = "Tron",
                FishColors = new List<string> { "Do trang" },
                FishQuantity = 6
            };

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
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var request = new CompatibilityRequest
            {
                DateOfBirth = 1990,
                IsMale = true,
                Direction = "Dong",
                PondShape = "Tron",
                FishColors = new List<string> { "Do trang" },
                FishQuantity = 6
            };

            var result = await service.AssessCompatibility(request);

            Assert.Equal(100.0, result.OverallCompatibilityScore);
        }

        [Fact]
        public async Task AssessCompatibility_NoDirectionMatch_ReturnsZeroDirectionScore()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var request = new CompatibilityRequest
            {
                DateOfBirth = 1990,
                IsMale = true,
                Direction = "NonExistent",
                PondShape = "Tron",
                FishColors = new List<string> { "Do trang" },
                FishQuantity = 6
            };

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.DirectionScore);
            Assert.False(result.OverallCompatibilityScore >= 100.0);
        }

        [Fact]
        public async Task AssessCompatibility_NoShapeMatch_ReturnsZeroShapeScore()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var request = new CompatibilityRequest
            {
                DateOfBirth = 1990,
                IsMale = true,
                Direction = "Dong",
                PondShape = "NonExistent",
                FishColors = new List<string> { "Do trang" },
                FishQuantity = 6
            };

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.ShapeScore);
        }

        [Fact]
        public async Task AssessCompatibility_NoQuantityMatch_ReturnsZeroQuantityScore()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var request = new CompatibilityRequest
            {
                DateOfBirth = 1990,
                IsMale = true,
                Direction = "Dong",
                PondShape = "Tron",
                FishColors = new List<string> { "Do trang" },
                FishQuantity = 5
            };

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.QuantityScore);
        }

        [Fact]
        public async Task AssessCompatibility_NoColorMatch_ReturnsZeroColorTotal()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var request = new CompatibilityRequest
            {
                DateOfBirth = 1990,
                IsMale = true,
                Direction = "Dong",
                PondShape = "Tron",
                FishColors = new List<string> { "Xanh" },
                FishQuantity = 6
            };

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.ColorScores["Xanh"]);
            Assert.Equal(0.0, result.ColorScores["TotalScore"]);
        }

        [Fact]
        public async Task AssessCompatibility_DifferentElement_ShapeAndDirectionNotMatch()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var request = new CompatibilityRequest
            {
                DateOfBirth = 1984,
                IsMale = true,
                Direction = "Dong",
                PondShape = "Tron",
                FishColors = new List<string> { "Do trang" },
                FishQuantity = 6
            };

            var result = await service.AssessCompatibility(request);

            Assert.Equal(0.0, result.DirectionScore);
            Assert.Equal(0.0, result.ShapeScore);
            Assert.Equal(0.0, result.QuantityScore);
        }

        [Fact]
        public async Task AssessCompatibility_InvalidYear_ThrowsException()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var request = new CompatibilityRequest
            {
                DateOfBirth = -1,
                IsMale = true,
                Direction = "Dong",
                PondShape = "Tron",
                FishColors = new List<string> { "Do trang" },
                FishQuantity = 6
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AssessCompatibility(request));
            Assert.Contains("date of birth", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task AssessCompatibility_ElementNotInDatabase_ThrowsArgumentException()
        {
            var options = CreateInMemoryOptions();
            var context = new KoiFengShuiContext(options);
            context.SaveChanges();
            var service = CreateService(context);

            var request = new CompatibilityRequest
            {
                DateOfBirth = 1990,
                IsMale = true,
                Direction = "Dong",
                PondShape = "Tron",
                FishColors = new List<string> { "Do trang" },
                FishQuantity = 6
            };

            var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.AssessCompatibility(request));
            Assert.Contains("element", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
