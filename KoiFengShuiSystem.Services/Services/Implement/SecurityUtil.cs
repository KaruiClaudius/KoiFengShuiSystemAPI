using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public static class SecurityUtil
    {
        private const int PasswordLength = 12;
        private const string UppercaseCharacters = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        private const string LowercaseCharacters = "abcdefghijkmnpqrstuvwxyz";
        private const string DigitCharacters = "23456789";
        private const string SpecialCharacters = "!@#$%^&*()-_=+";
        private const string AllCharacters =
            UppercaseCharacters + LowercaseCharacters + DigitCharacters + SpecialCharacters;

        public static string GenerateRandomPassword()
        {
            Span<char> passwordChars = stackalloc char[PasswordLength];

            passwordChars[0] = UppercaseCharacters[RandomNumberGenerator.GetInt32(UppercaseCharacters.Length)];
            passwordChars[1] = LowercaseCharacters[RandomNumberGenerator.GetInt32(LowercaseCharacters.Length)];
            passwordChars[2] = DigitCharacters[RandomNumberGenerator.GetInt32(DigitCharacters.Length)];
            passwordChars[3] = SpecialCharacters[RandomNumberGenerator.GetInt32(SpecialCharacters.Length)];

            for (var i = 4; i < PasswordLength; i++)
            {
                passwordChars[i] = AllCharacters[RandomNumberGenerator.GetInt32(AllCharacters.Length)];
            }

            for (var i = passwordChars.Length - 1; i > 0; i--)
            {
                var swapIndex = RandomNumberGenerator.GetInt32(i + 1);
                (passwordChars[i], passwordChars[swapIndex]) = (passwordChars[swapIndex], passwordChars[i]);
            }

            return new string(passwordChars);
        }
    }
}
