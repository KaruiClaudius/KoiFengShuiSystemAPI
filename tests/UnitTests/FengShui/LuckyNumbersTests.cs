using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;

namespace UnitTests.FengShui
{
    public class LuckyNumbersTests
    {
        [Theory]
        [InlineData("1,6", new[] { 1, 6 })]
        [InlineData(" 3 , 8 ", new[] { 3, 8 })]
        [InlineData("16", new[] { 6 })]
        [InlineData("9,11", new[] { 9, 1 })]
        [InlineData("", new int[0])]
        [InlineData(null, new int[0])]
        [InlineData("abc,5", new[] { 5 })]
        public void ParseLastDigitTargets_ParsesExactDigitSet(string? csv, int[] expected)
        {
            var result = LuckyNumbers.ParseLastDigitTargets(csv);

            Assert.Equal(expected.Length, result.Count);
            Assert.All(expected, digit => Assert.True(result.Contains(digit), $"Missing digit {digit}"));
        }

        [Fact]
        public void ParseLastDigitTargets_DoesNotSubstringMatch_NeighbouringDigits()
        {
            // Legacy code used string.Contains on the raw CSV; the digit-set model takes
            // the LAST digit of each parsed number. "23" therefore contributes exactly {3}.
            var targets = LuckyNumbers.ParseLastDigitTargets("23");

            Assert.Single(targets);
            Assert.True(targets.Contains(3));
            Assert.False(targets.Contains(2));
            Assert.False(targets.Contains(9));
        }

        [Theory]
        [InlineData("1,6", 6)]
        [InlineData("3,8", 8)]
        [InlineData("2,7,4", 4)]
        [InlineData(null, 9)]
        [InlineData("", 9)]
        [InlineData("abc", 9)]
        public void RecommendedQuantity_UsesFinalLuckyNumberDigit_WithFallbackNine(string? csv, int expected)
        {
            Assert.Equal(expected, LuckyNumbers.RecommendedQuantity(csv));
        }
    }
}
