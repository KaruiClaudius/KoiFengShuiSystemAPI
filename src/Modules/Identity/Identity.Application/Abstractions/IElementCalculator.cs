namespace KoiFengShuiSystem.Modules.Identity.Application.Abstractions;

/// <summary>
/// Anti-corruption port over the shared Feng Shui element (cung phi) calculation.
/// The application layer supplies a normalized gender flag; all feng shui domain
/// knowledge stays behind this seam.
/// </summary>
public interface IElementCalculator
{
    /// <summary>Returns the Vietnamese element name (Menh) for the given birth year and gender.</summary>
    string CalculateElement(int yearOfBirth, bool isMale);
}
