using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class CompatibilityService : ICompatibilityService
    {
        private readonly GenericRepository<CustomerFaP> _customerFaPRepository;
        private readonly GenericRepository<Element> _elementRepository;
        private readonly GenericRepository<FengShuiDirection> _fengShuiDirectionRepository;
        private readonly GenericRepository<ShapeCategory> _shapeCategoryRepository;
        private readonly GenericRepository<KoiBreed> _koiBreedRepository;
        private readonly GenericRepository<Direction> _directionRepository;
        private readonly GenericRepository<Recommendation> _recommendationRepository;
        private readonly GenericRepository<FishPond> _fishPondRepository;

        public CompatibilityService(
             GenericRepository<CustomerFaP> customerFaPRepository,
             GenericRepository<Element> elementRepository,
             GenericRepository<FengShuiDirection> fengShuiDirectionRepository,
             GenericRepository<ShapeCategory> shapeCategoryRepository,
             GenericRepository<KoiBreed> koiBreedRepository,
             GenericRepository<Direction> directionRepository,
             GenericRepository<Recommendation> recommendationRepository,
             GenericRepository<FishPond> fishPondRepository)
        {
            _customerFaPRepository = customerFaPRepository;
            _elementRepository = elementRepository;
            _fengShuiDirectionRepository = fengShuiDirectionRepository;
            _shapeCategoryRepository = shapeCategoryRepository;
            _koiBreedRepository = koiBreedRepository;
            _directionRepository = directionRepository;
            _recommendationRepository = recommendationRepository;
            _fishPondRepository = fishPondRepository;
        }

        public async Task<CompatibilityResponse> AssessCompatibility(CompatibilityRequest request)
        {
            var userElement = await GetElementFromDateOfBirth(request.DateOfBirth);

            var directionScore = await GetDirectionCompatibilityScore(request.Direction, userElement.ElementId);
            var shapeScore = await GetShapeCompatibilityScore(request.PondShape, userElement.ElementId);
            var colorScores = await GetColorCompatibilityScores(request.FishColors, userElement.ElementId);
            var quantityScore = await GetQuantityCompatibilityScore(request.FishQuantity, userElement.ElementId);

            var overallScore = CalculateOverallScore(directionScore, shapeScore, colorScores.Values.Average(), quantityScore);

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

        private async Task<Element> GetElementFromDateOfBirth(int yearOfBirth)
        {
            string elementName = CalculateElement(yearOfBirth);
            return await _elementRepository.FindAsync(e => e.ElementName == elementName);
        }

        private string CalculateElement(int yearOfBirth)
        {
            if (yearOfBirth <= 0)
            {
                throw new ArgumentException($"Invalid year of birth: {yearOfBirth}. Year must be a positive number.");
            }

            // Lấy 2 số cuối cùng của năm sinh
            int lastTwoDigits = yearOfBirth % 100;

            // Quy đổi Thiên Can
            int stem = yearOfBirth % 10;
            int stemValue = stem switch
            {
                0 or 1 => 4, // Canh, Tân
                2 or 3 => 5, // Nhâm, Quý
                4 or 5 => 1, // Giáp, Ất
                6 or 7 => 2, // Bính, Đinh
                8 or 9 => 3, // Mậu, Kỷ
                _ => throw new ArgumentException($"Invalid stem calculation for year: {yearOfBirth}")
            };

            // Quy đổi Địa Chi (dựa trên 2 số cuối của năm sinh chia cho 12)
            int branch = lastTwoDigits % 12;
            int branchValue = branch switch
            {
                4 or 5 or 10 or 11 => 0, // Tý, Sửu, Ngọ, Mùi
                0 or 1 or 6 or 7 => 1, // Dần, Mão, Thân, Dậu
                2 or 3 or 8 or 9 => 2, // Thìn, Tỵ, Tuất, Hợi
                _ => throw new ArgumentException($"Invalid branch calculation for year: {yearOfBirth}")
            };

            // Tính ngũ hành dựa trên giá trị Can và Chi
            int elementIndex = stemValue + branchValue;
            if (elementIndex > 5)
            {
                elementIndex -= 5;
            }

            string element = elementIndex switch
            {
                1 => "Kim",
                2 => "Thuỷ",
                3 => "Hoả",
                4 => "Thổ",
                5 => "Mộc",
                _ => throw new ArgumentException($"Invalid element calculation for year: {yearOfBirth}")
            };

            return element;
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
            var breeds = await _koiBreedRepository.GetAllAsync();
            var colorScores = new Dictionary<string, double>();
            var cleanedToOriginalMap = new Dictionary<string, string>();

            Console.WriteLine($"Calculating color scores for element ID: {elementId}");

            // Clean input colors and create a mapping
            var cleanedColors = colors.Select(color =>
            {
                var cleaned = CleanColorName(color);
                cleanedToOriginalMap[cleaned] = color;
                Console.WriteLine($"Cleaned color: {cleaned} (Original: {color})");
                return cleaned;
            }).Distinct().ToList();

            // Get all colors associated with the user's element
            var elementColors = breeds
                .Where(b => b.ElementId == elementId)
                .SelectMany(b => CleanColorName(b.Color).Split(' '))
                .Distinct()
                .ToList();

            Console.WriteLine($"Colors associated with element {elementId}: {string.Join(", ", elementColors)}");

            // Get the most common colors for the element (top 3)
            var recommendedColors = breeds
                .Where(b => b.ElementId == elementId)
                .SelectMany(b => CleanColorName(b.Color).Split(' '))
                .GroupBy(c => c)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .Select(g => g.Key)
                .ToList();

            Console.WriteLine($"Top 3 recommended colors: {string.Join(", ", recommendedColors)}");

            foreach (var cleanedColor in cleanedColors)
            {
                var originalColor = cleanedToOriginalMap[cleanedColor];

                if (recommendedColors.Contains(cleanedColor, StringComparer.OrdinalIgnoreCase))
                {
                    colorScores[originalColor] = 100.0;
                    Console.WriteLine($"Color {originalColor} scored 100.0 (Top recommended)");
                }
                else if (elementColors.Contains(cleanedColor, StringComparer.OrdinalIgnoreCase))
                {
                    colorScores[originalColor] = 75.0;
                    Console.WriteLine($"Color {originalColor} scored 75.0 (Matches element)");
                }
                else if (breeds.Any(b => CleanColorName(b.Color).Split(' ').Contains(cleanedColor, StringComparer.OrdinalIgnoreCase)))
                {
                    colorScores[originalColor] = 50.0;
                    Console.WriteLine($"Color {originalColor} scored 50.0 (Exists in breeds)");
                }
                else
                {
                    colorScores[originalColor] = 0.0;
                    Console.WriteLine($"Color {originalColor} scored 0.0 (No match found)");
                }
            }

            return colorScores;
        }

        private string CleanColorName(string color)
        {
            // Remove diacritics
            color = RemoveDiacritics(color);

            // Remove semicolons, extra whitespace, and the word "và"
            color = Regex.Replace(color, @"[;]|\s*va\s*|\s+", " ", RegexOptions.IgnoreCase).Trim();

            // Convert to lowercase
            return color.ToLowerInvariant();
        }

        private string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }


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

            if (directionScore < 50.0)
                recommendations.Add($"Hãy cân nhắc thay đổi hướng ao của bạn từ ({currentDirection}) thành ({await GetOptimalDirection(elementId)}) để tương thích tốt hơn.");
            else if (directionScore < 75.0)
                recommendations.Add($"Hướng của ao của bạn ({currentDirection}) nhìn chung là tương thích, nhưng có thể không tối ưu. Hãy cân nhắc điều chỉnh nó theo hướng {await GetOptimalDirection(elementId)}.");

            if (shapeScore < 50.0)
                recommendations.Add($"Hình dạng của ao của bạn ({currentShape}) có thể ảnh hưởng đáng kể đến khả năng tương thích. Hãy cân nhắc thay đổi nó thành ({await GetOptimalShape(elementId)}) để có sự hài hòa tốt hơn.");
            else if (shapeScore < 75.0)
                recommendations.Add($"Hình dạng ao của bạn ({currentShape}) nhìn chung là tương thích, nhưng có thể không lý tưởng. Hãy cân nhắc điều chỉnh thành ({await GetOptimalShape(elementId)}) để cải thiện sự cân bằng Phong thủy.");

            var averageColorScore = colorScores.Values.Average();
            if (averageColorScore < 50.0)
            {
                var recommendedColors = await GetRecommendedColors(elementId, 3); // Get top 3 recommended colors
                recommendations.Add($"Các màu Koi đã chọn ({string.Join(", ", currentColors)}) có thể không phù hợp nhất. Hãy cân nhắc khám phá các màu khác như ({string.Join(", ", recommendedColors)}) để có khả năng tương thích tốt hơn.");
            }
            else if (averageColorScore < 75.0)
            {
                var recommendedColors = await GetRecommendedColors(elementId, 2); // Get top 2 recommended colors
                var notRecommendedColors = currentColors.Where(c => colorScores[c] < 75.0).ToList();
                if (notRecommendedColors.Any())
                {
                    recommendations.Add($"Các màu Koi ({string.Join(", ", notRecommendedColors)}) có thể không tối ưu. Hãy cân nhắc thay thế bằng các màu như ({string.Join(", ", recommendedColors)}) để cải thiện sự hài hòa.");
                }
            }

            if (quantityScore < 25.0)
                recommendations.Add($"Số lượng cá trong ao của bạn ({currentQuantity}) có thể ảnh hưởng đáng kể đến khả năng tương thích. Hãy cân nhắc điều chỉnh số lượng thành {await GetRecommendedQuantity(elementId)} hoặc chữ số có hàng đơn vị là {await GetRecommendedQuantity(elementId)} để cân bằng Phong thủy tốt hơn.");
            else if (quantityScore < 50.0)
                recommendations.Add($"Số lượng cá trong ao của bạn ({currentQuantity}) có thể ảnh hưởng đến khả năng tương thích. Hãy cân nhắc điều chỉnh số lượng thành {await GetRecommendedQuantity(elementId)} hoặc chữ số có hàng đơn vị là {await GetRecommendedQuantity(elementId)} để cải thiện sự hài hòa.");

            return recommendations;
        }

        private async Task<string> GetOptimalDirection(int elementId)
        {
            try
            {
                var directions = await _directionRepository.GetAllAsync();

                Console.WriteLine($"Directions count: {directions.Count()}");

                if (!directions.Any())
                {
                    Console.WriteLine("No directions found. Returning Unknown.");
                    return "Unknown";
                }

                // Sort directions by some criteria (e.g., alphabetical order, popularity, etc.)
                var sortedDirections = directions.OrderBy(d => d.DirectionName).ToList();

                // Select the first direction as the optimal one
                var optimalDirection = sortedDirections.FirstOrDefault();

                if (optimalDirection == null)
                {
                    Console.WriteLine("Failed to select an optimal direction.");
                    return "Unknown";
                }

                return optimalDirection.DirectionName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred in GetOptimalDirection: {ex.Message}");
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
            var breeds = await _koiBreedRepository.GetAllAsync();

            var recommendedColors = breeds
                .Where(b => b.ElementId == elementId)
                .GroupBy(b => b.Color)
                .OrderByDescending(g => g.Count())
                .Take(count)
                .Select(g => g.Key)
                .ToList();

            return recommendedColors.Any() ? recommendedColors : new List<string> { "Unknown" };
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
