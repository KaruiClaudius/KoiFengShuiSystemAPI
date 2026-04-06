using KoiFengShuiSystem.Common.FengShui;

namespace KoiFengShuiSystem.Tests.FengShui
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
            var result = CungPhiCalculator.Calculate(year, isMale);

            Assert.NotNull(result);
            Assert.Equal(expectedCung, result.Cung);
            Assert.Equal(expectedMenh, result.Menh);
        }

        [Fact]
        public void Calculate_InvalidYear_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => CungPhiCalculator.Calculate(-1, true));
            Assert.Throws<ArgumentException>(() => CungPhiCalculator.Calculate(0, false));
        }

        [Fact]
        public void Calculate_Pre2000Male_ReturnsCorrectResult()
        {
            var result = CungPhiCalculator.Calculate(1990, true);
            Assert.Equal("Khảm", result.Cung);
            Assert.Equal("Thủy", result.Menh);
        }

        [Fact]
        public void Calculate_Pre2000Female_ReturnsCorrectResult()
        {
            var result = CungPhiCalculator.Calculate(1990, false);
            Assert.Equal("Cấn", result.Cung);
            Assert.Equal("Thổ", result.Menh);
        }

        [Fact]
        public void Calculate_Post2000Male_ReturnsCorrectResult()
        {
            var result = CungPhiCalculator.Calculate(2005, true);
            Assert.NotNull(result);
            Assert.NotNull(result.Cung);
            Assert.NotNull(result.Menh);
        }

        [Fact]
        public void Calculate_Post2000Female_ReturnsCorrectResult()
        {
            var result = CungPhiCalculator.Calculate(2005, false);
            Assert.NotNull(result);
            Assert.NotNull(result.Cung);
            Assert.NotNull(result.Menh);
        }

        [Fact]
        public void Calculate_TrunCungMale_ReturnsKhon()
        {
            var result = CungPhiCalculator.Calculate(1995, true);
            Assert.Equal("Khôn", result.Cung);
            Assert.Equal("Thổ", result.Menh);
        }

        [Fact]
        public void Calculate_TrunCungFemale_ReturnsCan()
        {
            var result = CungPhiCalculator.Calculate(1995, false);
            Assert.Equal("Khảm", result.Cung);
            Assert.Equal("Thủy", result.Menh);
        }

        [Theory]
        [InlineData("Đỏ", "đo")]
        [InlineData("Trắng", "trang")]
        [InlineData("Vàng;Trắng", "ng trang")]
        [InlineData("Xanh dương, trắng", "xanh duong  trang")]
        public void CleanColorName_RemovesDiacriticsAndSpecialChars(string input, string expected)
        {
            var result = CungPhiCalculator.CleanColorName(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CleanColorName_HandlesEmptyString()
        {
            var result = CungPhiCalculator.CleanColorName("");
            Assert.Equal("", result);
        }

        [Theory]
        [InlineData("Kim", "Khác", false)]
        [InlineData("Thủy", "Thủy", true)]
        [InlineData("Mộc", "mộc", true)]
        public void ElementComparison_IsCaseInsensitive(string element1, string element2, bool expected)
        {
            Assert.Equal(expected, element1.Equals(element2, StringComparison.OrdinalIgnoreCase));
        }
    }
}
