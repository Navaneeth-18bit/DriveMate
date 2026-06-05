using System;
using System.Security.Cryptography;
using System.Text;

namespace adminPage.Utilities
{
    public class PasswordHasher
    {
        private const int SaltSize = 128 / 8;
        private const int KeySize = 256 / 8;
        private const int Iterations = 10000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// Hash a password with a salt
        /// </summary>
        public static string HashPassword(string password)
        {
            using (var algorithm = new Rfc2898DeriveBytes(
                password,
                SaltSize,
                Iterations,
                Algorithm))
            {
                var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
                var salt = Convert.ToBase64String(algorithm.Salt);

                return $"{Iterations}.{salt}.{key}";
            }
        }

        /// <summary>
        /// Verify a password against its hash
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            try
            {
                var parts = hash.Split('.', 3);
                if (parts.Length != 3)
                {
                    return false;
                }

                var iterations = Convert.ToInt32(parts[0]);
                var salt = Convert.FromBase64String(parts[1]);
                var key = Convert.FromBase64String(parts[2]);

                using (var algorithm = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations,
                    Algorithm))
                {
                    var keyToCheck = algorithm.GetBytes(KeySize);
                    return CryptographicOperations.FixedTimeEquals(key, keyToCheck);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
