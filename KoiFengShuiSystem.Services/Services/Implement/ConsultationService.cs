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
    }
}


