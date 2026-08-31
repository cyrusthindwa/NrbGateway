using System.Security.Cryptography;
using System.Text;
using CHL.NrbGateway.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CHL.NrbGateway.Infrastructure.Services;

public class HmacService : IHmacService
{
    private readonly byte[] _keyBytes;

    public HmacService(IConfiguration configuration)
    {
        var hmacKeyStr = configuration["Encryption:HmacKey"] ?? throw new InvalidOperationException("Missing Encryption:HmacKey configuration.");
        _keyBytes = Encoding.UTF8.GetBytes(hmacKeyStr);
    }

    public string ComputeHmacSha256(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        using var hmac = new HMACSHA256(_keyBytes);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(input.Trim().ToUpperInvariant()));
        return Convert.ToHexStringLower(hashBytes);
    }
}

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var secret = configuration["Encryption:PgpSymKey"] ?? throw new InvalidOperationException("Missing Encryption:PgpSymKey configuration.");
        using var sha256 = SHA256.Create();
        _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length); // prepend IV

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        var fullBytes = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullBytes.Length - iv.Length];

        Array.Copy(fullBytes, 0, iv, 0, iv.Length);
        Array.Copy(fullBytes, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }
}
