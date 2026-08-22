using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace KoiFengShuiSystem.Modules.FengShui.Application.Calculations;

/// <summary>
/// Text normalization for koi color names (diacritic stripping and separator cleanup) used by
/// compatibility scoring. Kept near its consumers rather than in the domain model.
/// </summary>
public static class ColorNameCleaner
{
    public static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string CleanColorName(string color)
    {
        color = RemoveDiacritics(color);
        color = Regex.Replace(color, @"[;,\s]|\s*va\s*", " ", RegexOptions.IgnoreCase).Trim();
        return color.ToLowerInvariant();
    }
}
