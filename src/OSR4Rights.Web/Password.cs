using System;
using System.Linq;
using System.Security.Cryptography;

namespace OSR4Rights.Web
{
    // Using what asp.net identity uses under the hood
    // without the bits I don't need
    public static class Password
    {
        public static string HashPassword(this string password)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));

            var saltSize = 16; // 128 bit
            var keySize = 32; // 256 bit
            var iterations = 10000;

            using var algorithm = new Rfc2898DeriveBytes(password, saltSize, iterations, HashAlgorithmName.SHA512); var key = Convert.ToBase64String(algorithm.GetBytes(keySize));
            var salt = Convert.ToBase64String(algorithm.Salt);

            return $"{iterations}.{salt}.{key}";
        }

        public static bool HashMatches(this string password, string hashedPassword)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (hashedPassword == null) throw new ArgumentNullException(nameof(hashedPassword));

            var keySize = 32; // 256 bit

            var parts = hashedPassword.Split('.', 3);

            if (parts.Length != 3)
            {
                throw new FormatException("Unexpected hash format. " +
                                          "Should be formatted as `{iterations}.{salt}.{hash}`");
            }

            var iterations = Convert.ToInt32(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var key = Convert.FromBase64String(parts[2]);

            using var algorithm = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA512);
            var keyToCheck = algorithm.GetBytes(keySize);

            var verified = keyToCheck.SequenceEqual(key);

            return verified;
        }
    }
}
