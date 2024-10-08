using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
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

        public async Task<FengShuiResponse> GetFengShuiConsultationAsync(int yearOfBirth)
        {
            string elementName = CalculateElement(yearOfBirth);

            var element = await _elementRepository.FindAsync(e => e.ElementName == elementName);
            if (element == null)
            {
                throw new ArgumentException($"Element '{elementName}' not found for the given year of birth: {yearOfBirth}.");
            }

            var koiBreeds = await _koiBreedRepository.GetAllAsync();
            var shapeCategories = await _shapeCategoryRepository.GetAllAsync();
            var fengShuiDirections = await _fengShuiDirectionRepository.GetAllWithIncludeAsync(f => f.Direction);

            var matchingBreeds = koiBreeds.Where(k => k.ElementId == element.ElementId).ToList();
            var matchingShapes = shapeCategories.Where(s => s.ElementId == element.ElementId).ToList();
            var matchingDirections = fengShuiDirections.Where(f => f.ElementId == element.ElementId).ToList();

            return new FengShuiResponse
            {
                Element = element.ElementName,
                LuckyNumbers = element.LuckyNumber.Split(',').Select(n => n.Trim()).ToList() ?? new List<string>(),
                FishBreeds = matchingBreeds.Select(b => b.BreedName).ToList(),
                FishColors = matchingBreeds.Select(b => b.Color).Distinct().ToList(),
                SuggestedPonds = matchingShapes.Select(s => s.ShapeName).ToList(),
                SuggestedDirections = matchingDirections.Select(d => d.Direction?.DirectionName ?? "Unknown").ToList()
            };
        }

        private string CalculateElement(int yearOfBirth)
        {
            if (yearOfBirth <= 0)
            {
                throw new ArgumentException($"Invalid year of birth: {yearOfBirth}. Year must be a positive number.");
            }


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

            // Quy đổi Địa Chi
            int branch = yearOfBirth % 12;

            // Thân Dậu Tuất Hợi Tí Sửu Dần Mão Thìn Tị Ngọ Mùi
            // 0    1   2     3  4   5   6   7   8   8  10  11
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

            // Xác định element dựa trên kết quả tính toán
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

    }
}


