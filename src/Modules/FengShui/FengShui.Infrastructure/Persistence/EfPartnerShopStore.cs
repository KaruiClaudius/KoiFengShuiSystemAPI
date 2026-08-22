using KoiFengShuiSystem.Modules.FengShui.Application.Abstractions;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.FengShui.Infrastructure.Persistence
{
    public class EfPartnerShopStore : IPartnerShopStore
    {
        private readonly KoiFengShuiContext _context;

        public EfPartnerShopStore(KoiFengShuiContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
        }

        public async Task<IReadOnlyList<PartnerShop>> GetActiveAsync() =>
            await _context.PartnerShops
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();

        public async Task<PartnerShop?> GetByIdAsync(int id) =>
            await _context.PartnerShops.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);

        public async Task<PartnerShop> AddAsync(PartnerShop shop)
        {
            await _context.PartnerShops.AddAsync(shop);
            await _context.SaveChangesAsync();
            return shop;
        }

        public async Task UpdateAsync(PartnerShop shop)
        {
            _context.PartnerShops.Update(shop);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var shop = await _context.PartnerShops.FirstOrDefaultAsync(s => s.Id == id);
            if (shop == null)
            {
                return false;
            }

            _context.PartnerShops.Remove(shop);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
