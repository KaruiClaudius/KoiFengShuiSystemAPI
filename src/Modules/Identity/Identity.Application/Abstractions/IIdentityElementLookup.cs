namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

public interface IIdentityElementLookup
{
    Task<int?> GetElementIdByNameAsync(string elementName);

    Task<string?> GetElementNameByIdAsync(int elementId);
}
