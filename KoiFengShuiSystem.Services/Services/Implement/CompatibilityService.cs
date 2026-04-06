using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Common.FengShui;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class CompatibilityService : ICompatibilityService
    {
        private readonly GenericRepository<Element> _elementRepository;
        private readonly GenericRepository<FengShuiDirection> _fengShuiDirectionRepository;
        private readonly GenericRepository<ShapeCategory> _shapeCategoryRepository;
        private readonly GenericRepository<KoiBreed> _koiBreedRepository;
        private readonly GenericRepository<Direction> _directionRepository;
        private readonly GenericRepository<Recommendation> _recommendationRepository;
        private readonly GenericRepository<FishPond> _fishPondRepository;
        private readonly ILogger<CompatibilityService> _logger;

        public CompatibilityService(
             GenericRepository<Element> elementRepository,
             GenericRepository<FengShuiDirection> fengShuiDirectionRepository,
             GenericRepository<ShapeCategory> shapeCategoryRepository,
             GenericRepository<KoiBreed> koiBreedRepository,
             GenericRepository<Direction> directionRepository,
             GenericRepository<Recommendation> recommendationRepository,
             GenericRepository<FishPond> fishPondRepository,
             ILogger<CompatibilityService> logger)
        {
            _elementRepository = elementRepository;
            _fengShuiDirectionRepository = fengShuiDirectionRepository;
            _shapeCategoryRepository = shapeCategoryRepository;
            _koiBreedRepository = koiBreedRepository;
            _directionRepository = directionRepository;
            _recommendationRepository = recommendationRepository;
            _fishPondRepository = fishPondRepository;
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
                var cungPhiResult = CungPhiCalculator.Calculate(yearOfBirth, isMale);

                var element = await _elementRepository.FindAsync(e => e.ElementName == cungPhiResult.Menh);
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
            var directionEntity = await _directionRepository.FindAsync(d => d.DirectionName == direction);

            if (directionEntity == null)
            {
                return 0.0; // Direction not found
            }

            var fengShuiDirection = await _fengShuiDirectionRepository.FindAsync(
                f => f.DirectionId == directionEntity.DirectionId && f.ElementId == elementId);

            return fengShuiDirection != null ? 100.0 : 0.0;
        }

        private async Task<double> GetShapeCompatibilityScore(string shape, int elementId)
        {
            var shapeCategory = await _shapeCategoryRepository.FindAsync(s => s.ShapeName == shape && s.ElementId == elementId);
            return shapeCategory != null ? 100.0 : 0.0;
        }

        private async Task<Dictionary<string, double>> GetColorCompatibilityScores(List<string> colors, int elementId)
        {
            try
            {
                var breeds = await _koiBreedRepository.GetAllAsync();
                var colorScores = new Dictionary<string, double>();

                // Get recommended colors
                var recommendedColors = breeds
    .Where(b => b.ElementId == elementId)
    .SelectMany(b => CleanColorName(b.Color).Split(' '))
    .GroupBy(c => c)
    .OrderByDescending(g => g.Count())
    .Select(g => g.Key)
    .ToList();

                // Get element colors for semi-compatible check
                var elementColors = breeds
                    .Where(b => b.ElementId == elementId)
                    .SelectMany(b => CleanColorName(b.Color).Split(' '))
                    .Distinct()
                    .ToList();

                int colorCount = colors.Count;
                double exactIndividualScore = 100.0 / colorCount; // Calculate exact score per color
                double totalScore = 0;
                int fullyCompatibleCount = 0;

                // First pass: Count fully compatible colors and calculate preliminary scores
                _logger.LogDebug("Recommended Colors: {Colors}", string.Join(", ", recommendedColors));
                _logger.LogDebug("Element Colors: {Colors}", string.Join(", ", elementColors));

                foreach (var color in colors)
                {
                    var cleanedColor = CungPhiCalculator.CleanColorName(color);
                    _logger.LogDebug("Original Color: {Original}, Cleaned Color: {Cleaned}", color, cleanedColor);
                    double colorScore;

                    if (recommendedColors.Contains(cleanedColor, StringComparer.OrdinalIgnoreCase))
                    {
                        colorScore = exactIndividualScore;
                        fullyCompatibleCount++;
                        _logger.LogDebug("{Color} is fully compatible.", cleanedColor);
                    }
                    else if (elementColors.Contains(cleanedColor, StringComparer.OrdinalIgnoreCase))
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

                // Adjust for rounding errors if necessary
                if (fullyCompatibleCount > 0 && Math.Abs(totalScore - 100) < 0.1)
                {
                    // Calculate the adjustment needed
                    double adjustment = (100 - totalScore) / fullyCompatibleCount;

                    // Apply adjustment to fully compatible colors
                    foreach (var color in colors)
                    {
                        var cleanedColor = CleanColorName(color);
                        if (recommendedColors.Contains(cleanedColor, StringComparer.OrdinalIgnoreCase))
                        {
                            colorScores[color] = Math.Round(colorScores[color] + adjustment, 2);
                        }
                    }

                    totalScore = 100; // Set total score to exactly 100
                }

                // Add total score
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

        private string CleanColorName(string color) => CungPhiCalculator.CleanColorName(color);


        private async Task<double> GetQuantityCompatibilityScore(int quantity, int elementId)
        {
            // Extract the last digit of the quantity
            int lastDigit = Math.Abs(quantity % 10);

            // Convert the last digit to a string for comparison
            string lastDigitStr = lastDigit.ToString();

            // Find the element with the matching lucky number
            var matchingElement = await _elementRepository.FindAsync(e => e.LuckyNumber.Contains(lastDigitStr) && e.ElementId == elementId);

            if (matchingElement == null)
            {
                return 0.0; // Number not found or not matching the element
            }

            return 100.0; // Matching number found for the element
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

            // Direction recommendations
            if (directionScore < 50.0)
                recommendations.Add($"Hãy cân nhắc thay đổi hướng ao của bạn từ ({currentDirection}) thành ({await GetOptimalDirection(elementId)}) để tương thích tốt hơn.");
            else if (directionScore < 75.0)
                recommendations.Add($"Hướng của ao của bạn ({currentDirection}) nhìn chung là tương thích, nhưng có thể không tối ưu. Hãy cân nhắc điều chỉnh nó theo hướng {await GetOptimalDirection(elementId)}.");

            // Shape recommendations
            if (shapeScore < 50.0)
                recommendations.Add($"Hình dạng của ao của bạn ({currentShape}) có thể ảnh hưởng đáng kể đến khả năng tương thích. Hãy cân nhắc thay đổi nó thành ({await GetOptimalShape(elementId)}) để có sự hài hòa tốt hơn.");
            else if (shapeScore < 75.0)
                recommendations.Add($"Hình dạng ao của bạn ({currentShape}) nhìn chung là tương thích, nhưng có thể không lý tưởng. Hãy cân nhắc điều chỉnh thành ({await GetOptimalShape(elementId)}) để cải thiện sự cân bằng Phong thủy.");

            // Color recommendations 
            var totalColorScore = colorScores["TotalScore"];
            if (totalColorScore < 100.0) // Only show recommendations if not perfect score
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

            // Quantity recommendations
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
                // Get all feng shui directions compatible with the element
                var compatibleDirections = await _fengShuiDirectionRepository
                    .GetAllAsync(f => f.ElementId == elementId);

                if (!compatibleDirections.Any())
                {
                    return "Unknown";
                }

                // Join with directions to get direction names
                var directions = await _directionRepository.GetAllAsync();
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
                // Get all shape categories
                var shapeCategories = await _shapeCategoryRepository.GetAllAsync();

                // Filter shape categories by the user's element
                var compatibleShapes = shapeCategories.Where(s => s.ElementId == elementId).ToList();

                if (compatibleShapes.Any())
                {
                    // If there are compatible shapes, return the first one (or you could implement some priority logic here)
                    return compatibleShapes.First().ShapeName;
                }
                else
                {
                    // If no compatible shapes found for the user's element, return the first shape from all categories
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
                var breeds = await _koiBreedRepository.GetAllAsync();

                // First, normalize all breed colors
                var normalizedBreeds = breeds
                    .Where(b => b.ElementId == elementId)
                    .Select(b => new
                    {
                        OriginalColor = b.Color,
                        NormalizedColors = CleanColorName(b.Color)
                            .Split(' ')
                            .Where(c => !string.IsNullOrWhiteSpace(c))
                            .Distinct()
                            .ToList()
                    })
                    .ToList();

                // Get current most common color combinations
                var recommendedColors = normalizedBreeds
                    .GroupBy(b => b.OriginalColor)
                    .OrderByDescending(g => g.Count())
                    .Take(count)
                    .Select(g => g.Key)
                    .Where(color => !string.IsNullOrWhiteSpace(color))
                    .Distinct()
                    .ToList();

                // Filter out recommendations that are too similar to current colors
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
            var element = await _elementRepository.GetByIdAsync(elementId);

            if (element != null && !string.IsNullOrEmpty(element.LuckyNumber))
            {
                var luckyNumbers = element.LuckyNumber.Split(',').Select(n => n.Trim()).ToArray();

                if (luckyNumbers.Length > 0)
                {
                    var lastNumber = luckyNumbers.Last().Trim();

                    if (!string.IsNullOrEmpty(lastNumber) && int.TryParse(lastNumber, out int parsedNumber))
                    {
                        // Return the absolute value of the last number
                        return Math.Abs(parsedNumber % 10);
                    }
                }
            }

            // If the element doesn't exist or has no valid LuckyNumber, return a default value
            return 9; // You can adjust this default value as needed
        }

    }
}
