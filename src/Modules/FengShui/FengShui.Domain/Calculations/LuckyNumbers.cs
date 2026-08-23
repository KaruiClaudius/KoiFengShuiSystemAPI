namespace KoiFengShuiSystem.Modules.FengShui.Domain.Calculations
{
    /// <summary>
    /// Parses the comma-separated lucky-number string stored on <see cref="Entities.Element"/>
    /// into an explicit digit set so quantity compatibility checks compare digits exactly
    /// instead of relying on substring matching.
    /// </summary>
    public static class LuckyNumbers
    {
        /// <summary>
        /// Extracts the set of single-digit targets (last digit of each parsed number)
        /// considered lucky for an element. Duplicates collapse; unparsable tokens are ignored.
        /// </summary>
        public static IReadOnlySet<int> ParseLastDigitTargets(string? luckyNumberCsv)
        {
            var targets = new HashSet<int>();

            if (string.IsNullOrWhiteSpace(luckyNumberCsv))
            {
                return targets;
            }

            foreach (var token in luckyNumberCsv.Split(','))
            {
                var trimmed = token.Trim();

                if (int.TryParse(trimmed, out var number))
                {
                    targets.Add(Math.Abs(number % 10));
                }
            }

            return targets;
        }

        /// <summary>
        /// Returns the recommended fish quantity for an element: the last digit of the final
        /// lucky number, or the traditional fallback of 9 when nothing parseable exists.
        /// </summary>
        public static int RecommendedQuantity(string? luckyNumberCsv)
        {
            var lastToken = string.IsNullOrWhiteSpace(luckyNumberCsv)
                ? string.Empty
                : luckyNumberCsv.Split(',').Select(t => t.Trim()).LastOrDefault(t => !string.IsNullOrEmpty(t));

            return int.TryParse(lastToken, out var parsed)
                ? Math.Abs(parsed % 10)
                : 9;
        }
    }
}
