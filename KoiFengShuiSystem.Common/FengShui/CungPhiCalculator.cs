using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace KoiFengShuiSystem.Common.FengShui
{
    public record CungPhiResult(string Cung, string Menh, string Description);

    public static class CungPhiCalculator
    {
        private static readonly Dictionary<int, CungPhiResult> _cungPhiMap = new()
        {
            { 1, new CungPhiResult("Khảm", "Thủy", "Khảm - Hướng Bắc, thuộc hành Thủy, tượng trưng cho sự thông tuệ và khôn ngoan.") },
            { 2, new CungPhiResult("Khôn", "Thổ", "Khôn - Hướng Tây Nam, thuộc hành Thổ, tượng trưng cho sự ổn định và nuôi dưỡng.") },
            { 3, new CungPhiResult("Chấn", "Mộc", "Chấn - Hướng Đông, thuộc hành Mộc, tượng trưng cho sự phát triển và sinh sôi.") },
            { 4, new CungPhiResult("Tốn", "Mộc", "Tốn - Hướng Đông Nam, thuộc hành Mộc, tượng trưng cho sự mềm mại và uyển chuyển.") },
            { 5, new CungPhiResult("Trung cung", "Thổ", "Trung cung - Trung tâm, thuộc hành Thổ, tượng trưng cho sự cân bằng và hài hòa.") },
            { 6, new CungPhiResult("Càn", "Kim", "Càn - Hướng Tây Bắc, thuộc hành Kim, tượng trưng cho sự mạnh mẽ và quyết đoán.") },
            { 7, new CungPhiResult("Đoài", "Kim", "Đoài - Hướng Tây, thuộc hành Kim, tượng trưng cho sự vui vẻ và hạnh phúc.") },
            { 8, new CungPhiResult("Cấn", "Thổ", "Cấn - Hướng Đông Bắc, thuộc hành Thổ, tượng trưng cho sự kiên định và bền vững.") },
            { 9, new CungPhiResult("Ly", "Hoả", "Ly - Hướng Nam, thuộc hành Hỏa, tượng trưng cho sự sáng suốt và thông minh.") }
        };

        public static CungPhiResult Calculate(int yearOfBirth, bool isMale)
        {
            if (yearOfBirth <= 0)
            {
                throw new ArgumentException($"Invalid year of birth: {yearOfBirth}. Year must be a positive number.");
            }

            int lastTwoDigits = yearOfBirth % 100;
            int a = (lastTwoDigits / 10) + (lastTwoDigits % 10);
            if (a > 9)
            {
                a = (a / 10) + (a % 10);
            }

            int resultNumber;
            if (yearOfBirth < 2000)
            {
                if (isMale)
                {
                    resultNumber = 10 - a;
                }
                else
                {
                    resultNumber = 5 + a;
                    if (resultNumber > 9)
                    {
                        resultNumber = (resultNumber / 10) + (resultNumber % 10);
                    }
                }
            }
            else
            {
                if (isMale)
                {
                    resultNumber = 9 - a;
                    if (resultNumber == 0)
                    {
                        resultNumber = 9;
                    }
                }
                else
                {
                    resultNumber = 6 + a;
                    if (resultNumber > 9)
                    {
                        resultNumber = (resultNumber / 10) + (resultNumber % 10);
                    }
                }
            }

            if (resultNumber == 5)
            {
                resultNumber = isMale ? 2 : 8;
            }

            return _cungPhiMap[resultNumber];
        }

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
}
