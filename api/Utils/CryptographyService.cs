using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SmwHackTracker.api.Utils;

public class CryptographyService
{

    public static byte[] DeriveKeyFromMasterPassword(string masterPassword)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(masterPassword));

        var key = new byte[32];
        Array.Copy(hash, 0, key, 0, Math.Min(hash.Length, 32));
        
        return key;
    }

    public static string EncryptPassword(string plainTextPassword, byte[] key)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            throw new ArgumentException("Password cannot be null, empty, or whitespace");

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        aes.GenerateIV();
        
        using var memoryStream = new MemoryStream();

        memoryStream.Write(aes.IV, 0, aes.IV.Length);
        
        using var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using var writer = new StreamWriter(cryptoStream);
        
        writer.Write(plainTextPassword);
        writer.Flush();
        cryptoStream.FlushFinalBlock();
        
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    public static string DecryptPassword(string encryptedPassword, byte[] key)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            throw new ArgumentException("Encrypted password cannot be null or empty");

        var cipherData = Convert.FromBase64String(encryptedPassword);
        
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(cipherData, 0, iv, 0, iv.Length);
        aes.IV = iv;

        var encryptedData = new byte[cipherData.Length - iv.Length];
        Array.Copy(cipherData, iv.Length, encryptedData, 0, encryptedData.Length);
        
        using var memoryStream = new MemoryStream(encryptedData);
        using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);
        
        return reader.ReadToEnd();
    }

    public static string HashMasterPassword(string masterPassword, string salt)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = salt + masterPassword;
        var bytes = Encoding.UTF8.GetBytes(saltedPassword);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
} 