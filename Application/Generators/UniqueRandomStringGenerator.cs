using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Generators
{
    public static class UniqueRandomStringGenerator
    {
        private readonly static Random _random = new Random();
        private const string Letters = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const string LettersAndDigits = Letters + Digits;

        public static string GenerateUniqueRandomString(int length = 20)
        {
            if (length < 2)
                throw new ArgumentException("The length must be at least 2 to meet the requirements.");

            // Generate a GUID and convert it to a string
            string guidString = Guid.NewGuid().ToString("N"); // "N" format is 32 characters (digits and letters)

            // If the GUID is longer than the required length, truncate it
            if (guidString.Length > length - 1)
            {
                guidString = guidString.Substring(0, length - 1);
            }

            // Generate a string that starts with a letter
            char firstCharacter = Letters[_random.Next(Letters.Length)];

            // If additional random characters are needed (remaining length > 0)
            int remainingLength = length - guidString.Length - 1;
            string randomChars = remainingLength > 0
                ? new string(Enumerable.Range(0, remainingLength)
                    .Select(_ => LettersAndDigits[_random.Next(LettersAndDigits.Length)]).ToArray())
                : string.Empty;

            // Combine GUID part, starting letter, and remaining random characters
            return firstCharacter + guidString + randomChars;
        }
    }
}
