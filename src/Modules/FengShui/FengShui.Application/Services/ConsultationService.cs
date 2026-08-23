using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;
using KoiFengShuiSystem.Modules.FengShui.Application.Responses;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Services
{
    /// <summary>
    /// Produces a feng shui consultation from the caller's birth year. Reference tables
    /// load concurrently and are served warm by the caching read store; all shaping of
    /// the response happens in memory.
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly IFengShuiReadStore _readStore;
        private readonly ILogger<ConsultationService> _logger;

        public ConsultationService(IFengShuiReadStore readStore, ILogger<ConsultationService> logger)
        {
            _readStore = readStore;
            _logger = logger;
        }

        public async Task<FengShuiResponse> GetFengShuiConsultationAsync(int yearOfBirth, bool isMale)
        {
            try
            {
                var cungPhiResult = CungPhiCalculator.Calculate(yearOfBirth, isMale ? Gender.Male : Gender.Female);

                var elementTask = _readStore.GetElementByNameAsync(cungPhiResult.Menh);
                var shapesTask = _readStore.GetAllShapeCategoriesAsync();
                var breedsTask = _readStore.GetAllKoiBreedsAsync();
                var directionsTask = _readStore.GetAllFengShuiDirectionsWithDirectionAsync();

                await Task.WhenAll(elementTask, shapesTask, breedsTask, directionsTask);

                var element = await elementTask;
                if (element == null)
                {
                    throw new ArgumentException($"Element '{cungPhiResult.Menh}' not found.");
                }

                var (recommendedShapes, notRecommendedShapes) = ClassifyShapes(await shapesTask, element.ElementId);

                var matchingBreeds = (await breedsTask).Where(k => k.ElementId == element.ElementId).ToList();
                var matchingDirections = (await directionsTask)
                     .Where(f => f.ElementId == element.ElementId && f.Direction != null)
                     .Select(f => new DirectionRecommendation
                     {
                         DirectionName = f.Direction!.DirectionName ?? "Unknown",
                         Description = f.Description ?? "Không có mô tả",
                         IsRecommended = true
                     })
                     .ToList();

                return new FengShuiResponse
                {
                    Element = cungPhiResult.Menh,
                    Cung = cungPhiResult.Cung,
                    LuckyNumbers = ParseLuckyNumbers(element.LuckyNumber),
                    FishBreeds = matchingBreeds.Select(b => b.BreedName ?? "Unknown").ToList(),
                    FishColors = matchingBreeds.Select(b => b.Color ?? "Unknown").Distinct().ToList(),
                    SuggestedPonds = CreatePondRecommendations(recommendedShapes, notRecommendedShapes),
                    SuggestedDirections = matchingDirections
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing feng shui consultation for year {Year}, isMale {IsMale}", yearOfBirth, isMale);
                throw new ApplicationException("Error processing feng shui consultation", ex);
            }
        }

        private static List<string> ParseLuckyNumbers(string? luckyNumberCsv) =>
            string.IsNullOrWhiteSpace(luckyNumberCsv)
                ? new List<string>()
                : luckyNumberCsv.Split(',').Select(n => n.Trim()).ToList();

        private static (List<ShapeCategory> Recommended, List<ShapeCategory> NotRecommended) ClassifyShapes(
            IReadOnlyList<ShapeCategory> allShapes, int elementId)
        {
            var recommended = allShapes
                .Where(s => s.ElementId.HasValue && s.ElementId.Value == elementId)
                .ToList();

            var notRecommended = allShapes
                .Where(s => !s.ElementId.HasValue)
                .ToList();

            return (recommended, notRecommended);
        }

        private static List<PondShapeRecommendation> CreatePondRecommendations(
            List<ShapeCategory> recommendedShapes,
            List<ShapeCategory> notRecommendedShapes)
        {
            var recommendations = new List<PondShapeRecommendation>();

            recommendations.AddRange(recommendedShapes.Select(s => new PondShapeRecommendation
            {
                ShapeName = s.ShapeName,
                Description = s.Description ?? "Không có mô tả",
                IsRecommended = true
            }));

            recommendations.AddRange(notRecommendedShapes.Select(s => new PondShapeRecommendation
            {
                ShapeName = s.ShapeName,
                Description = $"KHÔNG NÊN SỬ DỤNG - {s.Description ?? "Không có mô tả"}",
                IsRecommended = false
            }));

            return recommendations;
        }
    }
}
