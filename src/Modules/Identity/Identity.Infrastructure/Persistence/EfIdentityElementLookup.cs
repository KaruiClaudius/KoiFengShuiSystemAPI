using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;

public class EfIdentityElementLookup : IIdentityElementLookup
{
    private readonly KoiFengShuiContext _context;

    public EfIdentityElementLookup(KoiFengShuiContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public async Task<int?> GetElementIdByNameAsync(string elementName)
    {
        return await _context.Elements
            .Where(element => element.ElementName == elementName)
            .Select(element => (int?)element.ElementId)
            .FirstOrDefaultAsync();
    }

    public async Task<string?> GetElementNameByIdAsync(int elementId)
    {
        return await _context.Elements
            .Where(element => element.ElementId == elementId)
            .Select(element => element.ElementName)
            .FirstOrDefaultAsync();
    }
}
