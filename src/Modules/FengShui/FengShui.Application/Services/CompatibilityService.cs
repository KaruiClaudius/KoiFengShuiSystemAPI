using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;
using KoiFengShuiSystem.Modules.FengShui.Application.Calculations;
using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Responses;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Services
{
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
            var userElement = await GetElementFromDateOfBirth(request.DateOfBirth, request.IsMale);

            if (userElement == null)
            {
                throw new ArgumentException($"Could not find element for date of birth {request.DateOfBirth} and gender {request.IsMale}");
            }

            var directionScore = await GetDirectionCompatibilityScore(request.Direction, userElement.ElementId);
            var shapeScore = await GetShapeCompatibilityScore(request.PondShape, userElement.ElementId);
            var colorScores = await GetColorCompatibilityScores(request.FishColors, userElement.ElementId);
            var quantityScore = await GetQuantityCompatibilityScore(request.FishQuantity, userElement.ElementId);

            var overallScore = CalculateOverallScore(directionScore, shapeScore, colorScores["TotalScore"], quantityScore);

            var recommendations = await GenerateRecommendations(
                directionScore, shapeScore, colorScores, quantityScore,
                request.Direction, request.PondShape, request.FishColors, request.FishQuantity,
                userElement.ElementId);

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

        private async Task<double> GetDirectionCompatibilityScore(string direction, int elementId)
        {
            var directionEntity = await _readStore.GetDirectionByNameAsync(direction);

            if (directionEntity == null)
            {
                return 0.0;
            }

            var fengShuiDirection = await _readStore.GetFengShuiDirectionAsync(directionEntity.DirectionId, elementId);

            return fengShuiDirection != null ? 100.0 : 0.0;
        }

        private async Task<double> GetShapeCompatibilityScore(string shape, int elementId)
        {
            var shapeCategory = await _readStore.GetShapeByNameAndElementIdAsync(shape, elementId);
            return shapeCategory != null ? 100.0 : 0.0;
        }

        private async Task<Dictionary<string, double>> GetColorCompatibilityScores(List<string> colors, int elementId)
        {
            try
            {
                var breeds = await _readStore.GetAllKoiBreedsAsync();
                var colorScores = new Dictionary<string, double>();

                var recommendedColors = breeds
                    .Where(b => b.ElementId == elementId)
                    .SelectMany(b => ColorNameCleaner.CleanColorName(b.Color).Split(' '))
                    .GroupBy(c => c)
                    .OrderByDescending(g => g.Count())
                    .Select(g => g.Key)
                    .ToList();

                var elementColors = breeds
                    .Where(b => b.ElementId == elementId)
                    .SelectMany(b => ColorNameCleaner.CleanColorName(b.Color).Split(' '))
                    .Distinct()
                    .ToList();

                int colorCount = colors.Count;
                double exactIndividualScore = 100.0 / colorCount;
                double totalScore = 0;
                int fullyCompatibleCount = 0;

                _logger.LogDebug("Recommended Colors: {Colors}", string.Join(", ", recommendedColors));
                _logger.LogDebug("Element Colors: {Colors}", string.Join(", ", elementColors));

                foreach (var color in colors)
                {
                    var cleanedColor = ColorNameCleaner.CleanColorName(color);
                    _logger.LogDebug("Original Color: {Original}, Cleaned Color: {Cleaned}", color, cleanedColor);
                    double colorScore;

                    var colorWords = cleanedColor.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (colorWords.Any(w => recommendedColors.Contains(w, StringComparer.OrdinalIgnoreCase)))
                    {
                        colorScore = exactIndividualScore;
                        fullyCompatibleCount++;
                        _logger.LogDebug("{Color} is fully compatible.", cleanedColor);
                    }
                    else if (colorWords.Any(w => elementColors.Contains(w, StringComparer.OrdinalIgnoreCase)))
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
                    double adjustment = (100 - totalScore) / fullyCompatibleCount;

                    foreach (var color in colors)
                    {
                        var cleanedColor = ColorNameCleaner.CleanColorName(color);
                        if (recommendedColors.Contains(cleanedColor, StringComparer.OrdinalIgnoreCase))
                        {
                            colorScores[color] = Math.Round(colorScores[color] + adjustment, 2);
                        }
                    }

                    totalScore = 100;
                }

                colorScores["TotalScore"] = Math.Round(totalScore, 2);

                return colorScores;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetColorCompatibilityScores");
                return new Dictionary<string, double>
                {
                    { "TotalScore", 0.0 }
                };
            }
        }

        private async Task<double> GetQuantityCompatibilityScore(int quantity, int elementId)
        {
            int lastDigit = Math.Abs(quantity % 10);
            string lastDigitStr = lastDigit.ToString();

            var matchingElement = await _readStore.GetElementByIdAsync(elementId);

            if (matchingElement == null || !matchingElement.LuckyNumber.Contains(lastDigitStr))
            {
                return 0.0;
            }

            return 100.0;
        }

        private double CalculateOverallScore(double directionScore, double shapeScore, double breedScore, double quantityScore)
        {
            return (directionScore + shapeScore + breedScore + quantityScore) / 4.0;
        }

        private async Task<List<string>> GenerateRecommendations(
            double directionScore, double shapeScore, Dictionary<string, double> colorScores, double quantityScore,
            string currentDirection, string currentShape, List<string> currentColors, int currentQuantity,
            int elementId)
        {
            var recommendations = new List<string>();

            if (directionScore < 50.0)
                recommendations.Add($"Hãy cân nhắc thay đổi hướng ao của bạn từ ({currentDirection}) thành ({await GetOptimalDirection(elementId)}) để tương thích tốt hơn.");
            else if (directionScore < 75.0)
                recommendations.Add($"Hướng của ao của bạn ({currentDirection}) nhìn chung là tương thích, nhưng có thể không tối ưu. Hãy cân nhắc điều chỉnh nó theo hướng {await GetOptimalDirection(elementId)}.");

            if (shapeScore < 50.0)
                recommendations.Add($"Hình dạng của ao của bạn ({currentShape}) có thể ảnh hưởng đáng kể đến khả năng tương thích. Hãy cân nhắc thay đổi nó thành ({await GetOptimalShape(elementId)}) để có sự hài hòa tốt hơn.");
            else if (shapeScore < 75.0)
                recommendations.Add($"Hình dạng ao của bạn ({currentShape}) nhìn chung là tương thích, nhưng có thể không lý tưởng. Hãy cân nhắc điều chỉnh thành ({await GetOptimalShape(elementId)}) để cải thiện sự cân bằng Phong thủy.");

            var totalColorScore = colorScores["TotalScore"];
            if (totalColorScore < 100.0)
            {
                var recommendedColors = await GetRecommendedColors(elementId, 3);
                var lowScoringColors = currentColors
                    .Where(c => colorScores.ContainsKey(c) && colorScores[c] < (100.0 / currentColors.Count))
                    .ToList();

                if (lowScoringColors.Any())
                {
                    recommendations.Add($"Các màu Koi ({string.Join(", ", lowScoringColors)}) có thể không tối ưu. Hãy cân nhắc thay thế bằng các màu như ({string.Join(", ", recommendedColors)}) để cải thiện sự hài hòa.");
                }
            }

            if (quantityScore < 25.0)
                recommendations.Add($"Số lượng cá trong ao của bạn ({currentQuantity}) có thể ảnh hưởng đáng kể đến khả năng tương thích. Hãy cân nhắc điều chỉnh số lượng thành ({await GetRecommendedQuantity(elementId)}) hoặc chữ số có hàng đơn vị là ({await GetRecommendedQuantity(elementId)}) để cân bằng Phong thủy tốt hơn.");
            else if (quantityScore < 50.0)
                recommendations.Add($"Số lượng cá trong ao của bạn ({currentQuantity}) có thể ảnh hưởng đến khả năng tương thích. Hãy cân nhắc điều chỉnh số lượng thành ({await GetRecommendedQuantity(elementId)}) hoặc chữ số có hàng đơn vị là ({await GetRecommendedQuantity(elementId)}) để cải thiện sự hài hòa.");

            return recommendations;
        }

        private async Task<string> GetOptimalDirection(int elementId)
        {
            try
            {
                var compatibleDirections = await _readStore.GetFengShuiDirectionsByElementIdAsync(elementId);

                if (!compatibleDirections.Any())
                {
                    return "Unknown";
                }

                var directions = await _readStore.GetAllDirectionsAsync();
                var optimalDirection = directions
                    .Join(compatibleDirections,
                        d => d.DirectionId,
                        f => f.DirectionId,
                        (d, f) => d.DirectionName)
                    .FirstOrDefault();

                return optimalDirection ?? "Unknown";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in GetOptimalDirection");
                return "Unknown";
            }
        }

        private async Task<string> GetOptimalShape(int elementId)
        {
            try
            {
                var shapeCategories = await _readStore.GetAllShapeCategoriesAsync();
                var compatibleShapes = shapeCategories.Where(s => s.ElementId == elementId).ToList();

                if (compatibleShapes.Any())
                {
                    return compatibleShapes.First().ShapeName;
                }
                else
                {
                    return shapeCategories.Any() ? shapeCategories.First().ShapeName : "Unknown";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in GetOptimalShape: {ex.Message}");
                return "Unknown";
            }
        }

        private async Task<List<string>> GetRecommendedColors(int elementId, int count)
        {
            try
            {
                var breeds = await _readStore.GetAllKoiBreedsAsync();

                var normalizedBreeds = breeds
                    .Where(b => b.ElementId == elementId)
                    .Select(b => new
                    {
                        OriginalColor = b.Color,
                        NormalizedColors = ColorNameCleaner.CleanColorName(b.Color)
                            .Split(' ')
                            .Where(c => !string.IsNullOrWhiteSpace(c))
                            .Distinct()
                            .ToList()
                    })
                    .ToList();

                var recommendedColors = normalizedBreeds
                    .GroupBy(b => b.OriginalColor)
                    .OrderByDescending(g => g.Count())
                    .Take(count)
                    .Select(g => g.Key)
                    .Where(color => !string.IsNullOrWhiteSpace(color))
                    .Distinct()
                    .ToList();

                return recommendedColors.Any() ? recommendedColors : new List<string> { "Unknown" };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetRecommendedColors: {ex.Message}");
                return new List<string> { "Unknown" };
            }
        }

        private async Task<int> GetRecommendedQuantity(int elementId)
        {
            var element = await _readStore.GetElementByIdAsync(elementId);

            if (element != null && !string.IsNullOrEmpty(element.LuckyNumber))
            {
                var luckyNumbers = element.LuckyNumber.Split(',').Select(n => n.Trim()).ToArray();

                if (luckyNumbers.Length > 0)
                {
                    var lastNumber = luckyNumbers.Last().Trim();

                    if (!string.IsNullOrEmpty(lastNumber) && int.TryParse(lastNumber, out int parsedNumber))
                    {
                        return Math.Abs(parsedNumber % 10);
                    }
                }
            }

            return 9;
        }
    }
}
