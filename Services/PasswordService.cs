using System.Security.Cryptography;
using System.Text;

namespace BusTicketBookingSystem.Services
{
    public class PasswordService : IPasswordService
    {
        public string GenerateTemporaryPassword(int length = 10)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$";
            var res = new StringBuilder();
            using var rng = RandomNumberGenerator.Create();
            var buffer = new byte[sizeof(uint)];

            while (res.Length < length)
            {
                rng.GetBytes(buffer);
                uint num = BitConverter.ToUInt32(buffer, 0);
                res.Append(valid[(int)(num % (uint)valid.Length)]);
            }
            return res.ToString();
        }

        public string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public bool VerifyPassword(string password, string hash)
        {
            return HashPassword(password) == hash;
        }
    }
}