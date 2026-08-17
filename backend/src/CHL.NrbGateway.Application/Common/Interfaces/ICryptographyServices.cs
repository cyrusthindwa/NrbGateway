namespace CHL.NrbGateway.Application.Common.Interfaces;

public interface IHmacService
{
    string ComputeHmacSha256(string input);
}

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
