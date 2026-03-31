using System;
using System.Security.Cryptography;
using System.Text;

namespace TravelAgency.Core.Services
{
    public static class PasswordHasher
    {
        public static string Hash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be empty.", nameof(password));

            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashBytes);
        }

        public static bool Verify(string password, string passwordHash)
        {
            var hashedInput = Hash(password);
            return hashedInput == passwordHash;
        }
    }
}