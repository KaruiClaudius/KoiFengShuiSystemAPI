using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Application.Requests;
using KoiFengShuiSystem.Modules.FengShui.Application.Responses;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Services
{
    public class PartnerShopService : IPartnerShopService
    {
        private readonly IPartnerShopStore _store;

        public PartnerShopService(IPartnerShopStore store)
        {
            _store = store;
        }

        public async Task<IReadOnlyList<PartnerShopResponse>> GetActiveAsync()
        {
            var shops = await _store.GetActiveAsync();
            return shops.Select(ToResponse).ToList();
        }

        public async Task<PartnerShopResponse> GetByIdAsync(int id)
        {
            var shop = await _store.GetByIdAsync(id);
            if (shop == null)
            {
                throw new KeyNotFoundException($"Partner shop not found. Id: {id}");
            }

            return ToResponse(shop);
        }

        public async Task<PartnerShopResponse> CreateAsync(PartnerShopRequest request)
        {
            ValidateRequest(request);

            var shop = new PartnerShop
            {
                CreatedAt = DateTime.UtcNow
            };
            ApplyRequest(shop, request);

            var added = await _store.AddAsync(shop);
            return ToResponse(added);
        }

        public async Task UpdateAsync(int id, PartnerShopRequest request)
        {
            ValidateRequest(request);

            var shop = await _store.GetByIdAsync(id);
            if (shop == null)
            {
                throw new KeyNotFoundException($"Partner shop not found. Id: {id}");
            }

            ApplyRequest(shop, request);

            await _store.UpdateAsync(shop);
        }

        public Task<bool> DeleteAsync(int id)
            => _store.DeleteAsync(id);

        private static void ApplyRequest(PartnerShop shop, PartnerShopRequest request)
        {
            shop.Name = request.Name;
            shop.Address = request.Address;
            shop.LinkUrl = request.LinkUrl;
            shop.Note = request.Note;
            shop.IsActive = request.IsActive;
        }

        private static void ValidateRequest(PartnerShopRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Name is required", nameof(request.Name));
            }

            if (string.IsNullOrWhiteSpace(request.LinkUrl))
            {
                throw new ArgumentException("LinkUrl is required", nameof(request.LinkUrl));
            }

            if (!Uri.TryCreate(request.LinkUrl, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("LinkUrl must be a valid absolute HTTP(S) URL", nameof(request.LinkUrl));
            }
        }

        private static PartnerShopResponse ToResponse(PartnerShop shop) => new()
        {
            Id = shop.Id,
            Name = shop.Name,
            Address = shop.Address,
            LinkUrl = shop.LinkUrl,
            Note = shop.Note,
            IsActive = shop.IsActive,
            CreatedAt = shop.CreatedAt
        };
    }
}
