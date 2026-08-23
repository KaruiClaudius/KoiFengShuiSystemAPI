using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

namespace KoiFengShuiSystem.Modules.Identity.Infrastructure.FengShui;

/// <summary>Adapter delegating to the canonical Feng Shui domain calculator.</summary>
public sealed class FengShuiElementCalculator : IElementCalculator
{
    public string CalculateElement(int yearOfBirth, bool isMale)
    {
        return CungPhiCalculator.Calculate(yearOfBirth, isMale ? Gender.Male : Gender.Female).Menh;
    }
}
