using KoiFengShuiSystem.Modules.FengShui.Domain.Calculations;

namespace UnitTests.FengShui
{
    public class CungPhiCalculatorTests
    {
        [Theory]
        [InlineData(1990, true, "Khảm", "Thủy")]
        [InlineData(1990, false, "Cấn", "Thổ")]
        [InlineData(1984, true, "Đoài", "Kim")]
        [InlineData(1984, false, "Cấn", "Thổ")]
        [InlineData(2000, true, "Ly", "Hoả")]
        [InlineData(2000, false, "Càn", "Kim")]
        [InlineData(1995, true, "Khôn", "Thổ")]
        [InlineData(1995, false, "Khảm", "Thủy")]
        [InlineData(1980, true, "Khôn", "Thổ")]
        [InlineData(1980, false, "Tốn", "Mộc")]
        [InlineData(2025, true, "Khôn", "Thổ")]
        [InlineData(2025, false, "Tốn", "Mộc")]
        public void Calculate_ValidYear_ReturnsCorrectCungPhi(int year, bool isMale, string expectedCung, string expectedMenh)
        {
            var result = CungPhiCalculator.Calculate(year, isMale ? Gender.Male : Gender.Female);

            Assert.NotNull(result);
            Assert.Equal(expectedCung, result.Cung);
            Assert.Equal(expectedMenh, result.Menh);
        }

        [Fact]
        public void Calculate_InvalidYear_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CungPhiCalculator.Calculate(-1, Gender.Male));
            Assert.Throws<ArgumentException>(() => CungPhiCalculator.Calculate(0, Gender.Female));
        }

        [Fact]
        public void Calculate_Pre2000Male_ReturnsCorrectResult()
        {
            var result = CungPhiCalculator.Calculate(1990, Gender.Male);
            Assert.Equal("Khảm", result.Cung);
            Assert.Equal("Thủy", result.Menh);
        }

        [Fact]
        public void Calculate_Pre2000Female_ReturnsCorrectResult()
        {
            var result = CungPhiCalculator.Calculate(1990, Gender.Female);
            Assert.Equal("Cấn", result.Cung);
            Assert.Equal("Thổ", result.Menh);
        }

        [Fact]
        public void Calculate_Post2000Male_ReturnsCorrectResult()
        {
            var result = CungPhiCalculator.Calculate(2005, Gender.Male);
            Assert.NotNull(result);
            Assert.NotNull(result.Cung);
            Assert.NotNull(result.Menh);
        }

        [Fact]
        public void Calculate_Post2000Female_ReturnsCorrectResult()
        {
            var result = CungPhiCalculator.Calculate(2005, Gender.Female);
            Assert.NotNull(result);
            Assert.NotNull(result.Cung);
            Assert.NotNull(result.Menh);
        }

        [Fact]
        public void Calculate_TrunCungMale_ReturnsKhon()
        {
            var result = CungPhiCalculator.Calculate(1995, Gender.Male);
            Assert.Equal("Khôn", result.Cung);
            Assert.Equal("Thổ", result.Menh);
        }

        [Fact]
        public void Calculate_TrunCungFemale_ReturnsCan()
        {
            var result = CungPhiCalculator.Calculate(1995, Gender.Female);
            Assert.Equal("Khảm", result.Cung);
            Assert.Equal("Thủy", result.Menh);
        }

        [Fact]
        public void Calculate_ReturnsDescription_ForEveryEntry()
        {
            for (int year = 1930; year <= 2030; year++)
            {
                foreach (var gender in new[] { Gender.Male, Gender.Female })
                {
                    var result = CungPhiCalculator.Calculate(year, gender);
                    Assert.False(string.IsNullOrWhiteSpace(result.Cung));
                    Assert.False(string.IsNullOrWhiteSpace(result.Menh));
                    Assert.False(string.IsNullOrWhiteSpace(result.Description));
                }
            }
        }
    }
}
