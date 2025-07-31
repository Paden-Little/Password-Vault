using Xunit;
using SmwHackTracker.api.Utils;
using System;
using System.Text;

namespace SmwHackTracker.api.Tests;

public class CryptographyServiceTests
{
    [Fact]
    public void DeriveKeyFromMasterPassword_ShouldReturn32ByteKey()
    {
        // Arrange
        var masterPassword = "mySecretMasterPassword123!";

        // Act
        var key = CryptographyService.DeriveKeyFromMasterPassword(masterPassword);

        // Assert
        Assert.NotNull(key);
        Assert.Equal(32, key.Length); // AES-256 requires 32-byte key
    }

    [Fact]
    public void DeriveKeyFromMasterPassword_SameMasterPassword_ShouldReturnSameKey()
    {
        // Arrange
        var masterPassword = "consistentPassword123";

        // Act
        var key1 = CryptographyService.DeriveKeyFromMasterPassword(masterPassword);
        var key2 = CryptographyService.DeriveKeyFromMasterPassword(masterPassword);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void DeriveKeyFromMasterPassword_DifferentMasterPasswords_ShouldReturnDifferentKeys()
    {
        // Arrange
        var masterPassword1 = "password123";
        var masterPassword2 = "differentPassword456";

        // Act
        var key1 = CryptographyService.DeriveKeyFromMasterPassword(masterPassword1);
        var key2 = CryptographyService.DeriveKeyFromMasterPassword(masterPassword2);

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Theory]
    [InlineData("simplePassword")]
    [InlineData("Complex!P@ssw0rd$123")]
    [InlineData("Special chars: !@#$%^&*()")]
    [InlineData("Unicode: 测试密码")]
    public void EncryptPassword_ShouldEncryptSuccessfully(string plainTextPassword)
    {
        // Arrange
        var masterPassword = "masterKey123";
        var key = CryptographyService.DeriveKeyFromMasterPassword(masterPassword);

        // Act
        var encryptedPassword = CryptographyService.EncryptPassword(plainTextPassword, key);

        // Assert
        Assert.NotNull(encryptedPassword);
        Assert.NotEmpty(encryptedPassword);
        Assert.NotEqual(plainTextPassword, encryptedPassword);
        
        // Encrypted password should be Base64 encoded
        Assert.True(IsBase64String(encryptedPassword));
    }

    [Theory]
    [InlineData("testPassword123")]
    [InlineData("Another!Complex@Password#456")]
    [InlineData("Short")]
    [InlineData("VeryLongPasswordThatExceedsNormalLengthToTestEncryptionCapabilities")]
    public void EncryptDecrypt_RoundTrip_ShouldReturnOriginalPassword(string originalPassword)
    {
        // Arrange
        var masterPassword = "masterKey456";
        var key = CryptographyService.DeriveKeyFromMasterPassword(masterPassword);

        // Act
        var encrypted = CryptographyService.EncryptPassword(originalPassword, key);
        var decrypted = CryptographyService.DecryptPassword(encrypted, key);

        // Assert
        Assert.Equal(originalPassword, decrypted);
    }

    [Fact]
    public void EncryptPassword_SamePassword_ShouldReturnDifferentCiphertext()
    {
        // Arrange (same password encrypted twice should give different results due to random IV)
        var plainTextPassword = "samePassword123";
        var masterPassword = "masterKey789";
        var key = CryptographyService.DeriveKeyFromMasterPassword(masterPassword);

        // Act
        var encrypted1 = CryptographyService.EncryptPassword(plainTextPassword, key);
        var encrypted2 = CryptographyService.EncryptPassword(plainTextPassword, key);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2); // Different due to random IV
        
        // But both should decrypt to same plaintext
        var decrypted1 = CryptographyService.DecryptPassword(encrypted1, key);
        var decrypted2 = CryptographyService.DecryptPassword(encrypted2, key);
        Assert.Equal(plainTextPassword, decrypted1);
        Assert.Equal(plainTextPassword, decrypted2);
    }

    [Fact]
    public void DecryptPassword_WithWrongKey_ShouldThrowException()
    {
        // Arrange
        var plainTextPassword = "secretPassword";
        var correctMasterPassword = "correctMaster";
        var wrongMasterPassword = "wrongMaster";
        
        var correctKey = CryptographyService.DeriveKeyFromMasterPassword(correctMasterPassword);
        var wrongKey = CryptographyService.DeriveKeyFromMasterPassword(wrongMasterPassword);
        
        var encrypted = CryptographyService.EncryptPassword(plainTextPassword, correctKey);

        // Act & Assert
        Assert.Throws<System.Security.Cryptography.CryptographicException>(() =>
            CryptographyService.DecryptPassword(encrypted, wrongKey)
        );
    }

    [Fact]
    public void EncryptPassword_NullOrEmptyPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var key = CryptographyService.DeriveKeyFromMasterPassword("masterPassword");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CryptographyService.EncryptPassword(null!, key));
        Assert.Throws<ArgumentException>(() => CryptographyService.EncryptPassword("", key));
        Assert.Throws<ArgumentException>(() => CryptographyService.EncryptPassword("   ", key));
    }

    [Fact]
    public void DecryptPassword_InvalidBase64_ShouldThrowException()
    {
        // Arrange
        var key = CryptographyService.DeriveKeyFromMasterPassword("masterPassword");
        var invalidBase64 = "NotValidBase64!@#";

        // Act & Assert
        Assert.Throws<FormatException>(() => CryptographyService.DecryptPassword(invalidBase64, key));
    }

    [Fact]
    public void HashMasterPassword_ShouldReturnConsistentHash()
    {
        // Arrange
        var masterPassword = "userMasterPassword";
        var salt = "staticSalt123";

        // Act
        var hash1 = CryptographyService.HashMasterPassword(masterPassword, salt);
        var hash2 = CryptographyService.HashMasterPassword(masterPassword, salt);

        // Assert
        Assert.Equal(hash1, hash2);
        Assert.NotNull(hash1);
        Assert.NotEmpty(hash1);
        Assert.True(IsBase64String(hash1));
    }

    [Fact]
    public void HashMasterPassword_DifferentPasswords_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password1 = "password123";
        var password2 = "differentPassword456";
        var salt = "commonSalt";

        // Act
        var hash1 = CryptographyService.HashMasterPassword(password1, salt);
        var hash2 = CryptographyService.HashMasterPassword(password2, salt);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashMasterPassword_DifferentSalts_ShouldReturnDifferentHashes()
    {
        // Arrange
        var password = "samePassword";
        var salt1 = "salt123";
        var salt2 = "differentSalt456";

        // Act
        var hash1 = CryptographyService.HashMasterPassword(password, salt1);
        var hash2 = CryptographyService.HashMasterPassword(password, salt2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    // Helper method to validate Base64 strings
    private static bool IsBase64String(string base64)
    {
        try
        {
            Convert.FromBase64String(base64);
            return true;
        }
        catch
        {
            return false;
        }
    }
} 