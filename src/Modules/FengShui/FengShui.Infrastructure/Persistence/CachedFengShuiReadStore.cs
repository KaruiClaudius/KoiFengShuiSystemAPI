using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace KoiFengShuiSystem.Modules.FengShui.Infrastructure.Persistence
{
    /// <summary>
    /// Decorating read store that caches the module's reference data (elements, directions,
    /// shape categories, koi breeds and their feng shui mappings). These tables change rarely,
    /// so a short absolute expiry keeps responses warm while still picking up admin edits.
    /// The decorated store remains the single source of truth; nothing here writes through.
    /// </summary>
    public class CachedFengShuiReadStore : IFengShuiReadStore
    {
        private const int DefaultLifetimeMinutes = 5;

        private readonly IFengShuiReadStore _inner;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _lifetime;

        public CachedFengShuiReadStore(IFengShuiReadStore inner, IMemoryCache cache, TimeSpan? lifetime = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _lifetime = lifetime ?? TimeSpan.FromMinutes(DefaultLifetimeMinutes);
        }

        public Task<Element?> GetElementByNameAsync(string elementName) =>
            CacheAsync($"elements:name:{elementName}", () => _inner.GetElementByNameAsync(elementName));

        public Task<IReadOnlyList<Element>> GetAllElementsAsync() =>
            CacheAsync("elements:all", _inner.GetAllElementsAsync);

        public Task<Direction?> GetDirectionByNameAsync(string directionName) =>
            CacheAsync($"directions:name:{directionName}", () => _inner.GetDirectionByNameAsync(directionName));

        public Task<FengShuiDirection?> GetFengShuiDirectionAsync(int directionId, int elementId) =>
            CacheAsync(
                $"fsdirections:{directionId}:{elementId}",
                () => _inner.GetFengShuiDirectionAsync(directionId, elementId));

        public Task<ShapeCategory?> GetShapeByNameAndElementIdAsync(string shapeName, int elementId) =>
            CacheAsync(
                $"shapes:{shapeName}:{elementId}",
                () => _inner.GetShapeByNameAndElementIdAsync(shapeName, elementId));

        public Task<IReadOnlyList<ShapeCategory>> GetAllShapeCategoriesAsync() =>
            CacheAsync("shapes:all", _inner.GetAllShapeCategoriesAsync);

        public Task<IReadOnlyList<KoiBreed>> GetAllKoiBreedsAsync() =>
            CacheAsync("breeds:all", _inner.GetAllKoiBreedsAsync);

        public Task<IReadOnlyList<FengShuiDirection>> GetAllFengShuiDirectionsWithDirectionAsync() =>
            CacheAsync("fsdirections:all:withdirection", _inner.GetAllFengShuiDirectionsWithDirectionAsync);

        public Task<Element?> GetElementByIdAsync(int elementId) =>
            CacheAsync($"elements:id:{elementId}", () => _inner.GetElementByIdAsync(elementId));

        public Task<IReadOnlyList<FengShuiDirection>> GetFengShuiDirectionsByElementIdAsync(int elementId) =>
            CacheAsync(
                $"fsdirections:byelement:{elementId}",
                () => _inner.GetFengShuiDirectionsByElementIdAsync(elementId));

        public Task<IReadOnlyList<Direction>> GetAllDirectionsAsync() =>
            CacheAsync("directions:all", _inner.GetAllDirectionsAsync);

        private async Task<T> CacheAsync<T>(string key, Func<Task<T>> factory)
        {
            return (await _cache.GetOrCreateAsync(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _lifetime;
                return factory();
            }))!;
        }
    }
}
