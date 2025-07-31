using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SmwHackTracker.api.Utils;

public class CryptographyService
{
    /// <summary>
    /// Derives AES key from master password using SHA1 hash (educational purposes only)
    /// </summary>
    public static byte[] DeriveKeyFromMasterPassword(string masterPassword)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(masterPassword));
        
        // AES requires 32 bytes (256-bit) key, SHA1 gives 20 bytes
        // Pad with zeros to reach 32 bytes (for educational purposes)
        var key = new byte[32];
        Array.Copy(hash, 0, key, 0, Math.Min(hash.Length, 32));
        
        return key;
    }

    /// <summary>
    /// Encrypts password using AES-256-CBC
    /// </summary>
    public static string EncryptPassword(string plainTextPassword, byte[] key)
    {
        if (string.IsNullOrWhiteSpace(plainTextPassword))
            throw new ArgumentException("Password cannot be null, empty, or whitespace");

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        // Generate random IV for each encryption
        aes.GenerateIV();
        
        using var memoryStream = new MemoryStream();
        
        // Prepend IV to encrypted data
        memoryStream.Write(aes.IV, 0, aes.IV.Length);
        
        using var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using var writer = new StreamWriter(cryptoStream);
        
        writer.Write(plainTextPassword);
        writer.Flush();
        cryptoStream.FlushFinalBlock();
        
        return Convert.ToBase64String(memoryStream.ToArray());
    }

    /// <summary>
    /// Decrypts password using AES-256-CBC
    /// </summary>
    public static string DecryptPassword(string encryptedPassword, byte[] key)
    {
        if (string.IsNullOrEmpty(encryptedPassword))
            throw new ArgumentException("Encrypted password cannot be null or empty");

        var cipherData = Convert.FromBase64String(encryptedPassword);
        
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        // Extract IV from the beginning of cipher data
        var iv = new byte[aes.BlockSize / 8];
        Array.Copy(cipherData, 0, iv, 0, iv.Length);
        aes.IV = iv;
        
        // Extract actual encrypted data
        var encryptedData = new byte[cipherData.Length - iv.Length];
        Array.Copy(cipherData, iv.Length, encryptedData, 0, encryptedData.Length);
        
        using var memoryStream = new MemoryStream(encryptedData);
        using var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);
        
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Hash master password for storage (using SHA256 for better security than SHA1)
    /// </summary>
    public static string HashMasterPassword(string masterPassword, string salt)
    {
        using var sha256 = SHA256.Create();
        var saltedPassword = salt + masterPassword;
        var bytes = Encoding.UTF8.GetBytes(saltedPassword);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
} 