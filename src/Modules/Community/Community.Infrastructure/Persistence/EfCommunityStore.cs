using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.Community.Infrastructure.Persistence
{
    public class EfCommunityStore : ICommunityStore
    {
        private readonly KoiFengShuiContext _context;

        public EfCommunityStore(KoiFengShuiContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            _context = context;
        }

        public async Task<IReadOnlyList<FAQ>> GetAllAsync() =>
            await _context.FAQs
                .AsNoTracking()
                .ToListAsync();

        public async Task<FAQ?> GetByIdAsync(int id) =>
            await _context.FAQs.AsNoTracking().FirstOrDefaultAsync(f => f.FAQId == id);

        public async Task<FAQ> AddAsync(FAQ faq)
        {
            await _context.FAQs.AddAsync(faq);
            await _context.SaveChangesAsync();
            return faq;
        }

        // Full-replace semantics for detached instances: GetByIdAsync reads with AsNoTracking, so the
        // service mutates an entity that is not tracked by this context. Update() re-attaches it and
        // marks every property modified, which preserves legacy FAQ behavior because the service only
        // rewrites Question/Answer/CreateAt on the instance it just fetched (AccountId keeps its
        // database value).
        public async Task UpdateAsync(FAQ faq)
        {
            _context.FAQs.Update(faq);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var affected = await _context.FAQs
                .Where(f => f.FAQId == id)
                .ExecuteDeleteAsync();
            return affected > 0;
        }
    }
}
