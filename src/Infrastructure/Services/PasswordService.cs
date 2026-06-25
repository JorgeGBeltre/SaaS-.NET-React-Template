using System.Security.Cryptography;
using System.Text;
using System.Linq;
using Application.Interfaces;

namespace Infrastructure.Services
{
    public class PasswordService : IPasswordHasher
    {
        public (byte[] Hash, byte[] Salt) CreateHash(string password)
        {
            using var hmac = new HMACSHA512();
            byte[] salt = hmac.Key;
            byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return (hash, salt);
        }

        public bool Verify(string password, byte[] hash, byte[] salt)
        {
            using var hmac = new HMACSHA512(salt);
            byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(hash);
        }
    }
}
