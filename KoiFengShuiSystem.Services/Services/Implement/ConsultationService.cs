using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Response;

public class ConsultationService : IConsultationService
{
    private readonly GenericRepository<Element> _elementRepository;
    private readonly GenericRepository<KoiBreed> _koiBreedRepository;
    private readonly GenericRepository<ShapeCategory> _shapeCategoryRepository;
    private readonly GenericRepository<FengShuiDirection> _fengShuiDirectionRepository;

    public ConsultationService(
        GenericRepository<Element> elementRepository,
        GenericRepository<KoiBreed> koiBreedRepository,
        GenericRepository<ShapeCategory> shapeCategoryRepository,
        GenericRepository<FengShuiDirection> fengShuiDirectionRepository)
    {
        _elementRepository = elementRepository;
        _koiBreedRepository = koiBreedRepository;
        _shapeCategoryRepository = shapeCategoryRepository;
        _fengShuiDirectionRepository = fengShuiDirectionRepository;
    }

    public async Task<FengShuiResponse> GetFengShuiConsultationAsync(int yearOfBirth, bool isMale)
    {
        try
        {
            // Tính cung phi
            var cungPhiResult = CalculateCungPhi(yearOfBirth, isMale);

            // Lấy element tương ứng với mệnh
            var element = await _elementRepository.FindAsync(e => e.ElementName == cungPhiResult.Menh);
            if (element == null)
            {
                throw new ArgumentException($"Element '{cungPhiResult.Menh}' not found.");
            }

            // Lấy tất cả shapes và phân loại
            var allShapes = await _shapeCategoryRepository.GetAllAsync();

            // Phân loại shapes thành recommended và not recommended
            var (recommendedShapes, notRecommendedShapes) = ClassifyShapes(allShapes, element.ElementId);

            // Lấy các thông tin khác
            var koiBreeds = await _koiBreedRepository.GetAllAsync();
            var fengShuiDirections = await _fengShuiDirectionRepository.GetAllWithIncludeAsync(f => f.Direction);

            var matchingBreeds = koiBreeds.Where(k => k.ElementId == element.ElementId).ToList();
            var matchingDirections = fengShuiDirections
                .Where(f => f.ElementId == element.ElementId && f.Direction != null)
                .Select(f => f.Direction)
                .ToList();

            return new FengShuiResponse
            {
                Element = cungPhiResult.Menh,
                Cung = cungPhiResult.Cung,
                LuckyNumbers = element.LuckyNumber?.Split(',').Select(n => n.Trim()).ToList() ?? new List<string>(),
                FishBreeds = matchingBreeds.Select(b => b.BreedName ?? "Unknown").ToList(),
                FishColors = matchingBreeds.Select(b => b.Color ?? "Unknown").Distinct().ToList(),
                SuggestedPonds = CreatePondRecommendations(recommendedShapes, notRecommendedShapes),
                SuggestedDirections = matchingDirections.Select(d => d.DirectionName ?? "Unknown").ToList()
            };
        }
        catch (Exception ex)
        {
            // Log error here
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

        // Add recommended shapes
        recommendations.AddRange(recommendedShapes.Select(s => new PondShapeRecommendation
        {
            ShapeName = s.ShapeName,
            Description = s.Description ?? "Không có mô tả",
            IsRecommended = true
        }));

        // Add not recommended shapes with warning
        recommendations.AddRange(notRecommendedShapes.Select(s => new PondShapeRecommendation
        {
            ShapeName = s.ShapeName,
            Description = $"KHÔNG NÊN SỬ DỤNG - {s.Description ?? "Không có mô tả"}",
            IsRecommended = false
        }));

        return recommendations;
    }


private class CungPhiResult
    {
        public string Cung { get; set; }
        public string Menh { get; set; }
    }

    private readonly Dictionary<int, CungPhiResult> _cungPhiMap = new Dictionary<int, CungPhiResult>
    {
        { 1, new CungPhiResult { Cung = "Khảm", Menh = "Thủy" } },
        { 2, new CungPhiResult { Cung = "Khôn", Menh = "Thổ" } },
        { 3, new CungPhiResult { Cung = "Chấn", Menh = "Mộc" } },
        { 4, new CungPhiResult { Cung = "Tốn", Menh = "Mộc" } },
        { 5, new CungPhiResult { Cung = "Trung cung", Menh = "Thổ" } },
        { 6, new CungPhiResult { Cung = "Càn", Menh = "Kim" } },
        { 7, new CungPhiResult { Cung = "Đoài", Menh = "Kim" } },
        { 8, new CungPhiResult { Cung = "Cấn", Menh = "Thổ" } },
        { 9, new CungPhiResult { Cung = "Ly", Menh = "Hoả" } }
    };

    private CungPhiResult CalculateCungPhi(int yearOfBirth, bool isMale)
    {
        if (yearOfBirth <= 0)
        {
            throw new ArgumentException($"Invalid year of birth: {yearOfBirth}. Year must be a positive number.");
        }

        // Lấy 2 số cuối của năm sinh
        int lastTwoDigits = yearOfBirth % 100;

        // Cộng 2 số cuối
        int a = (lastTwoDigits / 10) + (lastTwoDigits % 10);
        if (a > 9)
        {
            a = (a / 10) + (a % 10);
        }

        int resultNumber;
        if (yearOfBirth < 2000)
        {
            // Trước năm 2000
            if (isMale)
            {
                resultNumber = 10 - a;
            }
            else
            {
                resultNumber = 5 + a;
                if (resultNumber > 9)
                {
                    resultNumber = (resultNumber / 10) + (resultNumber % 10);
                }
            }
        }
        else
        {
            // Từ năm 2000 trở đi
            if (isMale)
            {
                resultNumber = 9 - a;
                if (resultNumber == 0)
                {
                    resultNumber = 9; // Cung Ly
                }
            }
            else
            {
                resultNumber = 6 + a;
                if (resultNumber > 9)
                {
                    resultNumber = (resultNumber / 10) + (resultNumber % 10);
                }
            }
        }

        return _cungPhiMap[resultNumber];
    }
}
