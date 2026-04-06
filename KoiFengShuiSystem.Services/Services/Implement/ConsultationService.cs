using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Common.FengShui;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class ConsultationService : IConsultationService
    {
        private readonly GenericRepository<Element> _elementRepository;
        private readonly GenericRepository<KoiBreed> _koiBreedRepository;
        private readonly GenericRepository<ShapeCategory> _shapeCategoryRepository;
        private readonly GenericRepository<FengShuiDirection> _fengShuiDirectionRepository;
        private readonly ILogger<ConsultationService> _logger;

        public ConsultationService(
            GenericRepository<Element> elementRepository,
            GenericRepository<KoiBreed> koiBreedRepository,
            GenericRepository<ShapeCategory> shapeCategoryRepository,
            GenericRepository<FengShuiDirection> fengShuiDirectionRepository,
            ILogger<ConsultationService> logger)
        {
            _elementRepository = elementRepository;
            _koiBreedRepository = koiBreedRepository;
            _shapeCategoryRepository = shapeCategoryRepository;
            _fengShuiDirectionRepository = fengShuiDirectionRepository;
            _logger = logger;
        }

        public async Task<FengShuiResponse> GetFengShuiConsultationAsync(int yearOfBirth, bool isMale)
        {
        try
        {
            var cungPhiResult = CungPhiCalculator.Calculate(yearOfBirth, isMale);

            var element = await _elementRepository.FindAsync(e => e.ElementName == cungPhiResult.Menh);
            if (element == null)
            {
                throw new ArgumentException($"Element '{cungPhiResult.Menh}' not found.");
            }

            var allShapes = await _shapeCategoryRepository.GetAllAsync();

            var (recommendedShapes, notRecommendedShapes) = ClassifyShapes(allShapes, element.ElementId);

            var koiBreeds = await _koiBreedRepository.GetAllAsync();
            var fengShuiDirections = await _fengShuiDirectionRepository.GetAllWithIncludeAsync(f => f.Direction);

            var matchingBreeds = koiBreeds.Where(k => k.ElementId == element.ElementId).ToList();
            var matchingDirections = fengShuiDirections
                 .Where(f => f.ElementId == element.ElementId && f.Direction != null)
                 .Select(f => new DirectionRecommendation
                 {
                     DirectionName = f.Direction.DirectionName ?? "Unknown",
                     Description = f.Description ?? "Không có mô tả",
                     IsRecommended = true

                 })
                 .ToList();

            return new FengShuiResponse
            {
                Element = cungPhiResult.Menh,
                Cung = cungPhiResult.Cung,
                LuckyNumbers = element.LuckyNumber?.Split(',').Select(n => n.Trim()).ToList() ?? new List<string>(),
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
    private (List<ShapeCategory> Recommended, List<ShapeCategory> NotRecommended) ClassifyShapes(
        List<ShapeCategory> allShapes, int elementId)
    {
        var recommended = allShapes
            .Where(s => s.ElementId.HasValue && s.ElementId.Value == elementId)
            .ToList();

        var notRecommended = allShapes
            .Where(s => !s.ElementId.HasValue)
            .ToList();

        return (recommended, notRecommended);
    }

    private List<PondShapeRecommendation> CreatePondRecommendations(
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
