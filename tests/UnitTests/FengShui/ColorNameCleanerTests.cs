using KoiFengShuiSystem.Modules.FengShui.Application.Calculations;

namespace UnitTests.FengShui
{
    public class ColorNameCleanerTests
    {
        [Theory]
        [InlineData("Đỏ", "đo")]
        [InlineData("Trắng", "trang")]
        [InlineData("Vàng;Trắng", "ng trang")]
        [InlineData("Xanh dương, trắng", "xanh duong  trang")]
        public void CleanColorName_RemovesDiacriticsAndSpecialChars(string input, string expected)
        {
            var result = ColorNameCleaner.CleanColorName(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void CleanColorName_HandlesEmptyString()
        {
            var result = ColorNameCleaner.CleanColorName("");
            Assert.Equal("", result);
        }
    }
}
