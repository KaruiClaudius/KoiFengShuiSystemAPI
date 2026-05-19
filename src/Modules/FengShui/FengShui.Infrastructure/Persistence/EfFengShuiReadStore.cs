using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.FengShui.Infrastructure.Persistence
{
    public class EfFengShuiReadStore : IFengShuiReadStore
    {
        private readonly KoiFengShuiContext _context;

        public EfFengShuiReadStore(KoiFengShuiContext context)
        {
            _context = context;
        }

        public Task<Element?> GetElementByNameAsync(string elementName) =>
            _context.Elements.FirstOrDefaultAsync(e => e.ElementName == elementName);

        public async Task<IReadOnlyList<Element>> GetAllElementsAsync() =>
            await _context.Elements.AsNoTracking().ToListAsync();

        public Task<Direction?> GetDirectionByNameAsync(string directionName) =>
            _context.Directions.FirstOrDefaultAsync(d => d.DirectionName == directionName);

        public Task<FengShuiDirection?> GetFengShuiDirectionAsync(int directionId, int elementId) =>
            _context.FengShuiDirections.FirstOrDefaultAsync(f => f.DirectionId == directionId && f.ElementId == elementId);

        public Task<ShapeCategory?> GetShapeByNameAndElementIdAsync(string shapeName, int elementId) =>
            _context.ShapeCategories.FirstOrDefaultAsync(s => s.ShapeName == shapeName && s.ElementId == elementId);

        public async Task<IReadOnlyList<ShapeCategory>> GetAllShapeCategoriesAsync() =>
            await _context.ShapeCategories.AsNoTracking().ToListAsync();

        public async Task<IReadOnlyList<KoiBreed>> GetAllKoiBreedsAsync() =>
            await _context.KoiBreeds.AsNoTracking().ToListAsync();

        public async Task<IReadOnlyList<FengShuiDirection>> GetAllFengShuiDirectionsWithDirectionAsync() =>
            await _context.FengShuiDirections.Include(f => f.Direction).AsNoTracking().ToListAsync();

        public Task<Element?> GetElementByIdAsync(int elementId) =>
            _context.Elements.FirstOrDefaultAsync(e => e.ElementId == elementId);

        public async Task<IReadOnlyList<FengShuiDirection>> GetFengShuiDirectionsByElementIdAsync(int elementId) =>
            await _context.FengShuiDirections.Where(f => f.ElementId == elementId).AsNoTracking().ToListAsync();

        public async Task<IReadOnlyList<Direction>> GetAllDirectionsAsync() =>
            await _context.Directions.AsNoTracking().ToListAsync();
    }
}
