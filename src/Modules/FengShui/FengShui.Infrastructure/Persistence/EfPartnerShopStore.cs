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

        // Full-replace semantics for detached instances: GetByIdAsync reads with AsNoTracking, so the
        // service mutates an entity that is not tracked by this context. Update() re-attaches it and
        // marks every property modified, which is safe today because PartnerShopRequest covers all
        // mutable columns and CreatedAt is intentionally preserved from the fetched (immutable) value.
        public async Task UpdateAsync(PartnerShop shop)
        {
            _context.PartnerShops.Update(shop);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var affected = await _context.PartnerShops
                .Where(s => s.Id == id)
                .ExecuteDeleteAsync();
            return affected > 0;
        }
    }
}
