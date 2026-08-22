using System.ComponentModel.DataAnnotations;
using KoiFengShuiSystem.Shared.Models.Request;

namespace UnitTests.Admin
{
    public class FaqRequestValidationTests
    {
        private static bool IsValid(FAQRequest request)
        {
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();
            return Validator.TryValidateObject(request, context, results, validateAllProperties: true);
        }

        [Fact]
        public void Question_Empty_IsInvalid()
        {
            var request = new FAQRequest { Question = string.Empty, Answer = "Answer" };

            Assert.False(IsValid(request));
        }

        [Fact]
        public void Question_Null_IsInvalid()
        {
            var request = new FAQRequest { Question = null!, Answer = "Answer" };

            Assert.False(IsValid(request));
        }

        [Fact]
        public void Question_ExceedsMaxLength_IsInvalid()
        {
            var request = new FAQRequest { Question = new string('q', 1001) };

            Assert.False(IsValid(request));
        }

        [Fact]
        public void Answer_ExceedsMaxLength_IsInvalid()
        {
            var request = new FAQRequest { Question = "Valid question?", Answer = new string('a', 2001) };

            Assert.False(IsValid(request));
        }

        [Fact]
        public void ValidQuestionAndAnswer_PassesValidation()
        {
            var request = new FAQRequest { Question = "Valid question?", Answer = "Valid answer" };

            Assert.True(IsValid(request));
        }

        [Fact]
        public void Answer_Optional_MayBeEmpty()
        {
            var request = new FAQRequest { Question = "Valid question?", Answer = string.Empty };

            Assert.True(IsValid(request));
        }
    }
}
