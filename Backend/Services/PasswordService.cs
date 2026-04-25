using System.Security.Cryptography;
using System.Text;

namespace Backend.Services
{
    public static class PasswordService
    {
        public static (byte[] PasswordHash, byte[] PasswordSalt) CreatePasswordHash(string password)
        {
            using var hmac = new HMACSHA512();
            byte[] passwordSalt = hmac.Key;
            byte[] passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return (passwordHash, passwordSalt);
        }

        public static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using var hmac = new HMACSHA512(passwordSalt);
            byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }
    }
}
