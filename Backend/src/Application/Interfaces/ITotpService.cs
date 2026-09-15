namespace Application.Interfaces
{
    public interface ITotpService
    {
        (string Secret, string QrCodeUri) GenerateSecret(string email);
        bool VerifyCode(string secret, string code);
    }
}
