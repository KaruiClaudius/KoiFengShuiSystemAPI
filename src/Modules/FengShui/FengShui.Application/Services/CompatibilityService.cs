using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;
using KoiFengShuiSystem.Modules.FengShui.Application.Calculations;
using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Responses;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Services
{
    /// <summary>
    /// Scores a pond setup against the caller's element using a single reference-data
    /// snapshot. All scoring and recommendation text is computed in memory; no store
    /// call happens more than once per assessment.
    /// </summary>
    public class CompatibilityService : ICompatibilityService
    {
        private readonly IFengShuiReadStore _readStore;
        private readonly ILogger<CompatibilityService> _logger;

        public CompatibilityService(
             IFengShuiReadStore readStore,
             ILogger<CompatibilityService> logger)
        {
            _readStore = readStore;
            _logger = logger;
        }

        public async Task<CompatibilityResponse> AssessCompatibility(CompatibilityRequest request)
        {
            var element = await GetElementFromDateOfBirth(request.DateOfBirth, request.IsMale);

            var snapshot = await LoadReferenceData();

            var directionScore = ScoreDirection(request.Direction, element.ElementId, snapshot);
            var shapeScore = ScoreShape(request.PondShape, element.ElementId, snapshot);
            var colorScores = ScoreColors(request.FishColors, element.ElementId, snapshot);
            var quantityScore = ScoreQuantity(request.FishQuantity, element);

            var overallScore = CalculateOverallScore(directionScore, shapeScore, colorScores["TotalScore"], quantityScore);

            var recommendations = BuildRecommendations(
                snapshot,
                element,
                currentDirection: request.Direction,
                currentShape: request.PondShape,
                currentColors: request.FishColors,
                currentQuantity: request.FishQuantity,
                directionScore,
                shapeScore,
                colorScores,
                quantityScore);

            return new CompatibilityResponse
            {
                OverallCompatibilityScore = overallScore,
                DirectionScore = directionScore,
                ShapeScore = shapeScore,
                ColorScores = colorScores,
                QuantityScore = quantityScore,
                Recommendations = recommendations
            };
        }

        private async Task<Element> GetElementFromDateOfBirth(int yearOfBirth, bool isMale)
        {
            try
            {
                var cungPhiResult = CungPhiCalculator.Calculate(yearOfBirth, isMale ? Gender.Male : Gender.Female);

                var element = await _readStore.GetElementByNameAsync(cungPhiResult.Menh);
                if (element == null)
                {
                    throw new ArgumentException($"Could not find element with name {cungPhiResult.Menh}");
                }

                return element;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting element from date of birth: {YearOfBirth}, {IsMale}", yearOfBirth, isMale);
                throw new ArgumentException($"Error getting element from date of birth: {ex.Message}");
            }
        }

        private sealed record ReferenceSnapshot(
            IReadOnlyList<Direction> Directions,
            IReadOnlyList<FengShuiDirection> FengShuiDirections,
            IReadOnlyList<ShapeCategory> Shapes,
            IReadOnlyList<KoiBreed> Breeds);

        /// <summary>
        /// Loads every reference table needed for one assessment. The four loads are
        /// independent, so they run concurrently; the decorating cache makes warm calls free.
        /// </summary>
        private async Task<ReferenceSnapshot> LoadReferenceData()
        {
            var directionsTask = _readStore.GetAllDirectionsAsync();
            var fengShuiDirectionsTask = _readStore.GetAllFengShuiDirectionsWithDirectionAsync();
            var shapesTask = _readStore.GetAllShapeCategoriesAsync();
            var breedsTask = _readStore.GetAllKoiBreedsAsync();

            await Task.WhenAll(directionsTask, fengShuiDirectionsTask, shapesTask, breedsTask);

            return new ReferenceSnapshot(
                await directionsTask,
                await fengShuiDirectionsTask,
                await shapesTask,
                await breedsTask);
        }

        private static double ScoreDirection(string direction, int elementId, ReferenceSnapshot snapshot)
        {
            var directionEntity = snapshot.Directions.FirstOrDefault(
                d => string.Equals(d.DirectionName, direction, StringComparison.OrdinalIgnoreCase));

            if (directionEntity == null)
            {
                return 0.0;
            }

            var compatible = snapshot.FengShuiDirections.Any(
                f => f.DirectionId == directionEntity.DirectionId && f.ElementId == elementId);

            return compatible ? 100.0 : 0.0;
        }

        private static double ScoreShape(string shape, int elementId, ReferenceSnapshot snapshot)
        {
            var match = snapshot.Shapes.Any(
                s => s.ElementId == elementId &&
                     string.Equals(s.ShapeName, shape, StringComparison.OrdinalIgnoreCase));

            return match ? 100.0 : 0.0;
        }

        private Dictionary<string, double> ScoreColors(List<string> colors, int elementId, ReferenceSnapshot snapshot)
        {
            var breedWordsByColor = ExtractBreedColorWords(elementId, snapshot);
            var recommendedWords = breedWordsByColor
                .SelectMany(x => x.Words)
                .GroupBy(w => w, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();
            var elementWords = breedWordsByColor
                .SelectMany(x => x.Words)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            int colorCount = colors.Count;
            double exactIndividualScore = 100.0 / colorCount;
            double totalScore = 0;
            int fullyCompatibleCount = 0;
            var colorScores = new Dictionary<string, double>();

            foreach (var color in colors)
            {
                var cleanedColor = ColorNameCleaner.CleanColorName(color);
                var colorWords = cleanedColor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                double colorScore;

                if (colorWords.Any(w => recommendedWords.Contains(w, StringComparer.OrdinalIgnoreCase)))
                {
                    colorScore = exactIndividualScore;
                    fullyCompatibleCount++;
                    _logger.LogDebug("{Color} is fully compatible.", cleanedColor);
                }
                else if (colorWords.Any(w => elementWords.Contains(w, StringComparer.OrdinalIgnoreCase)))
                {
                    colorScore = exactIndividualScore / 2;
                    _logger.LogDebug("{Color} is semi-compatible.", cleanedColor);
                }
                else
                {
                    colorScore = 0;
                    _logger.LogDebug("{Color} is not compatible.", cleanedColor);
                }

                colorScores[color] = Math.Round(colorScore, 2);
                totalScore += colorScore;
            }

            if (fullyCompatibleCount > 0 && Math.Abs(totalScore - 100) < 0.1)
            {
                var adjustment = (100 - totalScore) / fullyCompatibleCount;

                foreach (var color in colors)
                {
                    var cleanedColor = ColorNameCleaner.CleanColorName(color);
                    if (recommendedWords.Contains(cleanedColor, StringComparer.OrdinalIgnoreCase))
                    {
                        colorScores[color] = Math.Round(colorScores[color] + adjustment, 2);
                    }
                }

                totalScore = 100;
            }

            colorScores["TotalScore"] = Math.Round(totalScore, 2);

            return colorScores;
        }

        /// <summary>
        /// Cleans and splits each element-matched breed's colour words exactly once per
        /// assessment; both scoring and colour recommendations reuse this result.
        /// </summary>
        private static List<(string OriginalColor, List<string> Words)> ExtractBreedColorWords(
            int elementId, ReferenceSnapshot snapshot)
        {
            return snapshot.Breeds
                .Where(b => b.ElementId == elementId)
                .Select(b => (
                    OriginalColor: b.Color ?? string.Empty,
                    Words: ColorNameCleaner.CleanColorName(b.Color ?? string.Empty)
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()))
                .Where(x => x.Words.Count > 0)
                .ToList();
        }

        private static double ScoreQuantity(int quantity, Element element)
        {
            var lastDigit = Math.Abs(quantity % 10);
            var luckyDigits = LuckyNumbers.ParseLastDigitTargets(element.LuckyNumber);

            return luckyDigits.Contains(lastDigit) ? 100.0 : 0.0;
        }

        private static double CalculateOverallScore(double directionScore, double shapeScore, double breedScore, double quantityScore)
        {
            return (directionScore + shapeScore + breedScore + quantityScore) / 4.0;
        }

        private List<string> BuildRecommendations(
            ReferenceSnapshot snapshot,
            Element element,
            string currentDirection,
            string currentShape,
            List<string> currentColors,
            int currentQuantity,
            double directionScore,
            double shapeScore,
            Dictionary<string, double> colorScores,
            double quantityScore)
        {
            var optimalDirection = ResolveOptimalDirection(element.ElementId, snapshot);
            var optimalShape = ResolveOptimalShape(element.ElementId, snapshot);
            var recommendedColors = ResolveRecommendedColors(element.ElementId, snapshot, count: 3);
            var recommendedQuantity = LuckyNumbers.RecommendedQuantity(element.LuckyNumber);

            var recommendations = new List<string>();

            if (directionScore < 50.0)
                recommendations.Add($"Hãy cân nhắc thay đổi hướng ao của bạn từ ({currentDirection}) thành ({optimalDirection}) để tương thích tốt hơn.");
            else if (directionScore < 75.0)
                recommendations.Add($"Hướng của ao của bạn ({currentDirection}) nhìn chung là tương thích, nhưng có thể không tối ưu. Hãy cân nhắc điều chỉnh nó theo hướng {optimalDirection}.");

            if (shapeScore < 50.0)
                recommendations.Add($"Hình dạng của ao của bạn ({currentShape}) có thể ảnh hưởng đáng kể đến khả năng tương thích. Hãy cân nhắc thay đổi nó thành ({optimalShape}) để có sự hài hòa tốt hơn.");
            else if (shapeScore < 75.0)
                recommendations.Add($"Hình dạng ao của bạn ({currentShape}) nhìn chung là tương thích, nhưng có thể không lý tưởng. Hãy cân nhắc điều chỉnh thành ({optimalShape}) để cải thiện sự cân bằng Phong thủy.");

            var totalColorScore = colorScores["TotalScore"];
            if (totalColorScore < 100.0)
            {
                var lowScoringColors = currentColors
                    .Where(c => colorScores.ContainsKey(c) && colorScores[c] < (100.0 / currentColors.Count))
                    .ToList();

                if (lowScoringColors.Any())
                {
                    recommendations.Add($"Các màu Koi ({string.Join(", ", lowScoringColors)}) có thể không tối ưu. Hãy cân nhắc thay thế bằng các màu như ({string.Join(", ", recommendedColors)}) để cải thiện sự hài hòa.");
                }
            }

            if (quantityScore < 25.0)
                recommendations.Add(QuantityAdvice(currentQuantity, recommendedQuantity, strong: true));
            else if (quantityScore < 50.0)
                recommendations.Add(QuantityAdvice(currentQuantity, recommendedQuantity, strong: false));

            return recommendations;
        }

        private static string QuantityAdvice(int currentQuantity, int recommendedQuantity, bool strong)
        {
            var impact = strong
                ? "có thể ảnh hưởng đáng kể đến khả năng tương thích"
                : "có thể ảnh hưởng đến khả năng tương thích";
            var goal = strong
                ? "để cân bằng Phong thủy tốt hơn"
                : "để cải thiện sự hài hòa";

            return $"Số lượng cá trong ao của bạn ({currentQuantity}) {impact}. Hãy cân nhắc điều chỉnh số lượng thành ({recommendedQuantity}) hoặc chữ số có hàng đơn vị là ({recommendedQuantity}) {goal}.";
        }

        private static string ResolveOptimalDirection(int elementId, ReferenceSnapshot snapshot)
        {
            var compatibleDirectionIds = snapshot.FengShuiDirections
                .Where(f => f.ElementId == elementId)
                .Select(f => f.DirectionId)
                .ToHashSet();

            if (compatibleDirectionIds.Count == 0)
            {
                return "Unknown";
            }

            var optimal = snapshot.Directions
                .Where(d => compatibleDirectionIds.Contains(d.DirectionId))
                .Select(d => d.DirectionName)
                .FirstOrDefault();

            return optimal ?? "Unknown";
        }

        private static string ResolveOptimalShape(int elementId, ReferenceSnapshot snapshot)
        {
            var compatibleShapes = snapshot.Shapes.Where(s => s.ElementId == elementId).ToList();

            if (compatibleShapes.Count > 0)
            {
                return compatibleShapes[0].ShapeName ?? "Unknown";
            }

            return snapshot.Shapes.Count > 0 ? snapshot.Shapes[0].ShapeName ?? "Unknown" : "Unknown";
        }

        private static List<string> ResolveRecommendedColors(int elementId, ReferenceSnapshot snapshot, int count)
        {
            var recommendedColors = snapshot.Breeds
                .Where(b => b.ElementId == elementId && !string.IsNullOrWhiteSpace(b.Color))
                .GroupBy(b => b.Color!.Trim())
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .Where(color => !string.IsNullOrWhiteSpace(color))
                .Distinct()
                .ToList();

            return recommendedColors.Count > 0 ? recommendedColors : new List<string> { "Unknown" };
        }
    }
}
