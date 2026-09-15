using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Infrastructure.Security
{
    public class OtpService : IOtpService
    {
        private readonly IOtpCodeRepository _otpRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OtpService(IOtpCodeRepository otpRepository, IUnitOfWork unitOfWork)
        {
            _otpRepository = otpRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> GenerateOtpAsync(int userId, OtpPurpose purpose, string? ipAddress = null)
        {
            // 1. Invalidate any active OTPs for this purpose
            await _otpRepository.InvalidatePreviousOtpsAsync(userId, purpose);

            // 2. Generate cryptographically secure 6-digit code
            var plainCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // 3. Hash code with SHA256
            var codeHash = HashCode(plainCode);

            // 4. Expiry: 15 mins for password reset, 5 mins for email verification
            var ttlMinutes = purpose == OtpPurpose.PasswordReset ? 15 : 5;

            var otp = new OtpCode
            {
                UserId = userId,
                CodeHash = codeHash,
                Purpose = purpose,
                ExpiresAt = DateTime.UtcNow.AddMinutes(ttlMinutes),
                Attempts = 0,
                MaxAttempts = 3,
                IpAddress = ipAddress
            };

            _otpRepository.Add(otp);
            await _unitOfWork.SaveChangesAsync();

            return plainCode;
        }

        public async Task<bool> VerifyOtpAsync(int userId, OtpPurpose purpose, string code)
        {
            var otp = await _otpRepository.GetActiveOtpAsync(userId, purpose);
            if (otp == null)
            {
                return false;
            }

            if (otp.Attempts >= otp.MaxAttempts)
            {
                return false;
            }

            var inputHash = HashCode(code);
            bool isMatch = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(inputHash),
                Encoding.UTF8.GetBytes(otp.CodeHash));

            if (!isMatch)
            {
                otp.Attempts++;
                _otpRepository.Update(otp);
                await _unitOfWork.SaveChangesAsync();
                return false;
            }

            // OTP verified successfully -> mark used
            otp.UsedAt = DateTime.UtcNow;
            _otpRepository.Update(otp);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private static string HashCode(string code)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(code));
            return Convert.ToHexString(bytes);
        }
    }
}
