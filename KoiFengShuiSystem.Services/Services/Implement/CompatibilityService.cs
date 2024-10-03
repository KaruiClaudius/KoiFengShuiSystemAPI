using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var colorScore = await GetColorCompatibilityScore(request.FishColor, userElement.ElementId);
            var quantityScore = await GetQuantityCompatibilityScore(request.FishQuantity, userElement.ElementId);

            var overallScore = CalculateOverallScore(directionScore, shapeScore, colorScore, quantityScore);

            var recommendations = await GenerateRecommendations(
                directionScore, shapeScore, colorScore, quantityScore,
                request.Direction, request.PondShape, request.FishColor, request.FishQuantity,
                userElement.ElementId);

            return new CompatibilityResponse
            {
                OverallCompatibilityScore = overallScore,
                DirectionScore = directionScore,
                ShapeScore = shapeScore,
                ColorScore = colorScore,
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

            int stem = yearOfBirth % 10;
            int branch = yearOfBirth % 12;

            int elementIndex = (stem + branch) % 5;
            if (elementIndex == 0) elementIndex = 5;

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

        private async Task<double> GetColorCompatibilityScore(string color, int elementId)
        {
            var compatibleBreeds = await _koiBreedRepository.GetAllAsync();
            var matchingBreeds = compatibleBreeds.Where(b => b.Color.Equals(color, StringComparison.OrdinalIgnoreCase) && b.ElementId == elementId).ToList();

            if (matchingBreeds.Any())
            {
                return 100.0; // Full compatibility if there's a match
            }
            else
            {
                // If no exact match, check if there are any breeds with the same color (regardless of element)
                var sameColorBreeds = compatibleBreeds.Where(b => b.Color.Equals(color, StringComparison.OrdinalIgnoreCase)).ToList();
                if (sameColorBreeds.Any())
                {
                    return 50.0; // Partial compatibility if the color exists but doesn't match the element
                }
                else
                {
                    return 0.0; // No compatibility if the color doesn't exist in the database
                }
            }
        }

        private async Task<double> GetQuantityCompatibilityScore(int quantity, int elementId)
        {
            var customerFaP = await _customerFaPRepository.FindAsync(c => c.ElementId == elementId && c.FishQuantity == quantity);
            return customerFaP != null ? customerFaP.Percentage * 100.0 : 0.0;
        }

        private double CalculateOverallScore(double directionScore, double shapeScore, double breedScore, double quantityScore)
        {
            return (directionScore + shapeScore + breedScore + quantityScore) / 4.0;
        }

        private async Task<List<string>> GenerateRecommendations(
       double directionScore, double shapeScore, double colorScore, double quantityScore,
       string currentDirection, string currentShape, string currentColor, int currentQuantity,
       int elementId)
        {
            var recommendations = new List<string>();

            if (directionScore < 50.0)
                recommendations.Add($"Hãy cân nhắc thay đổi hướng ao của bạn từ {currentDirection} thành {await GetOptimalDirection(elementId)} để tương thích tốt hơn.");
            else if (directionScore < 75.0)
                recommendations.Add($"Hướng của ao của bạn ({currentDirection}) nhìn chung là tương thích, nhưng có thể không tối ưu. Hãy cân nhắc điều chỉnh nó theo hướng {await GetOptimalDirection(elementId)}.");

            if (shapeScore < 50.0)
                recommendations.Add($"Hình dạng của ao của bạn ({currentShape}) có thể ảnh hưởng đáng kể đến khả năng tương thích. Hãy cân nhắc thay đổi nó thành {await GetOptimalShape(elementId)} để có sự hài hòa tốt hơn.");
            else if (shapeScore < 75.0)
                recommendations.Add($"Hình dạng ao của bạn ({currentShape}) nhìn chung là tương thích, nhưng có thể không lý tưởng. Hãy cân nhắc điều chỉnh thành {await GetOptimalShape(elementId)} để cải thiện sự cân bằng Phong thủy.");

            if (colorScore < 50.0)
                recommendations.Add($"Koi đã chọn màu ({currentColor}) có thể không phù hợp nhất. Hãy cân nhắc khám phá các màu khác như {await GetRecommendedColors(elementId)} để có khả năng tương thích tốt hơn.");
            else if (colorScore < 75.0)
                recommendations.Add($"Koi đã chọn màu ({currentColor}) nhìn chung là tương thích, nhưng có thể không tối ưu. Hãy cân nhắc khám phá các màu khác như {await GetRecommendedColors(elementId)} để cải thiện sự hài hòa.");

            if (quantityScore < 25.0)
                recommendations.Add($"Số lượng cá trong ao của bạn ({currentQuantity}) có thể ảnh hưởng đáng kể đến khả năng tương thích. Hãy cân nhắc điều chỉnh số lượng thành {await GetRecommendedQuantity(elementId)} để cân bằng Phong thủy tốt hơn.");
            else if (quantityScore < 50.0)
                recommendations.Add($"Số lượng cá trong ao của bạn ({currentQuantity}) có thể ảnh hưởng đến khả năng tương thích. Hãy cân nhắc điều chỉnh số lượng thành {await GetRecommendedQuantity(elementId)} để cải thiện sự hài hòa.");

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
            var recommendations = await _recommendationRepository.GetAllAsync();
            var ponds = await _fishPondRepository.GetAllAsync();
            var shapes = await _shapeCategoryRepository.GetAllAsync();

            var optimalShape = recommendations
                .GroupJoin(ponds, r => r.PondId, p => p.PondId, (r, p) => new { Recommendation = r, Ponds = p })
                .SelectMany(x => x.Ponds.DefaultIfEmpty(), (x, p) => new { x.Recommendation, Pond = p })
                .GroupJoin(shapes, x => x.Pond.ShapeId, s => s.ShapeId, (x, s) => new { x.Recommendation, x.Pond, Shapes = s })
                .SelectMany(x => x.Shapes.DefaultIfEmpty(), (x, s) => new { x.Recommendation, x.Pond, Shape = s })
                .GroupBy(x => x.Shape.ShapeName)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return optimalShape?.Key ?? "Unknown";
        }

        private async Task<List<string>> GetRecommendedColors(int elementId, int topCount = 3)
        {
            var breeds = await _koiBreedRepository.GetAllAsync();

            var recommendedColors = breeds
                .Where(b => b.ElementId == elementId)
                .GroupBy(b => b.Color)
                .OrderByDescending(g => g.Count())
                .Take(topCount)
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
