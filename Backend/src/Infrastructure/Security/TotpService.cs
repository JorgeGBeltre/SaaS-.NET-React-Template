using System;
using Application.Interfaces;
using OtpNet;

namespace Infrastructure.Security
{
    public class TotpService : ITotpService
    {
        private const string Issuer = "SaaS-Template";

        public (string Secret, string QrCodeUri) GenerateSecret(string email)
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            var secret = Base32Encoding.ToString(key);
            var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(Issuer)}:{Uri.EscapeDataString(email)}?secret={secret}&issuer={Uri.EscapeDataString(Issuer)}&algorithm=SHA1&digits=6&period=30";
            return (secret, qrCodeUri);
        }

        public bool VerifyCode(string secret, string code)
        {
            if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(code))
                return false;

            try
            {
                var secretBytes = Base32Encoding.ToBytes(secret);
                var totp = new Totp(secretBytes);
                return totp.VerifyTotp(code.Trim(), out _, VerificationWindow.RfcSpecifiedNetworkDelay);
            }
            catch
            {
                return false;
            }
        }
    }
}
