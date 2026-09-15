namespace Application.Interfaces
{
    public interface IPasswordHasher
    {
        (byte[] Hash, byte[] Salt) CreateHash(string password);
        bool Verify(string password, byte[] hash, byte[] salt);
    }
}
