using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using OwlNet.Infrastructure.Services;
using Shouldly;

namespace OwlNet.Tests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="EncryptionService"/> using the ephemeral data protection provider.
/// Each test creates its own SUT instance to ensure complete isolation.
/// </summary>
public sealed class EncryptionServiceTests
{
    private readonly EncryptionService _sut;

    public EncryptionServiceTests()
    {
        var provider = new EphemeralDataProtectionProvider();
        _sut = new EncryptionService(provider, NullLogger<EncryptionService>.Instance);
    }

    // ──────────────────────────────────────────────
    // Encrypt
    // ──────────────────────────────────────────────

    [Fact]
    public void Encrypt_ValidPlainText_ReturnsEncryptedString()
    {
        // Arrange
        var plainText = "sk-test-api-key-12345";

        // Act
        var result = _sut.Encrypt(plainText);

        // Assert
        result.ShouldSatisfyAllConditions(
            () => result.ShouldNotBeNullOrWhiteSpace(),
            () => result.ShouldNotBe(plainText)
        );
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        var plainText = string.Empty;

        // Act
        var result = _sut.Encrypt(plainText);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Encrypt_NullString_ReturnsEmptyString()
    {
        // Arrange
        string plainText = null!;

        // Act
        var result = _sut.Encrypt(plainText);

        // Assert
        result.ShouldBe(string.Empty);
    }

    // ──────────────────────────────────────────────
    // TryDecrypt
    // ──────────────────────────────────────────────

    [Fact]
    public void TryDecrypt_ValidCipherText_ReturnsTrueAndDecryptedText()
    {
        // Arrange
        var originalText = "sk-test-api-key-12345";
        var cipherText = _sut.Encrypt(originalText);

        // Act
        var success = _sut.TryDecrypt(cipherText, out var plainText);

        // Assert
        success.ShouldBeTrue();
        plainText.ShouldBe(originalText);
    }

    [Fact]
    public void TryDecrypt_EmptyCipherText_ReturnsTrueAndEmptyString()
    {
        // Arrange
        var cipherText = string.Empty;

        // Act
        var success = _sut.TryDecrypt(cipherText, out var plainText);

        // Assert
        success.ShouldBeTrue();
        plainText.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryDecrypt_NullCipherText_ReturnsTrueAndEmptyString()
    {
        // Arrange
        string cipherText = null!;

        // Act
        var success = _sut.TryDecrypt(cipherText, out var plainText);

        // Assert
        success.ShouldBeTrue();
        plainText.ShouldBe(string.Empty);
    }

    [Fact]
    public void TryDecrypt_InvalidCipherText_ReturnsFalseAndNull()
    {
        // Arrange — garbage string that was never produced by Encrypt
        var invalidCipherText = "this-is-not-a-valid-encrypted-string";

        // Act
        var success = _sut.TryDecrypt(invalidCipherText, out var plainText);

        // Assert
        success.ShouldBeFalse();
        plainText.ShouldBeNull();
    }

    [Fact]
    public void TryDecrypt_DifferentProtectorInstance_ReturnsFalseAndNull()
    {
        // Arrange — encrypt with one ephemeral provider, try to decrypt with another
        var otherProvider = new EphemeralDataProtectionProvider();
        var otherService = new EncryptionService(otherProvider, NullLogger<EncryptionService>.Instance);

        var cipherText = otherService.Encrypt("secret-data");

        // Act — decrypt with the original SUT (different key material)
        var success = _sut.TryDecrypt(cipherText, out var plainText);

        // Assert
        success.ShouldBeFalse();
        plainText.ShouldBeNull();
    }

    // ──────────────────────────────────────────────
    // Round-trip
    // ──────────────────────────────────────────────

    [Theory]
    [InlineData("simple-key")]
    [InlineData("sk-or-v1-abc123def456ghi789jkl012mno345pqr678stu901vwx234")]
    [InlineData("key with spaces and special chars !@#$%^&*()")]
    [InlineData("a")] // single character
    public void EncryptThenDecrypt_VariousInputs_RoundTripsSuccessfully(string originalText)
    {
        // Arrange
        var cipherText = _sut.Encrypt(originalText);

        // Act
        var success = _sut.TryDecrypt(cipherText, out var decryptedText);

        // Assert
        success.ShouldBeTrue();
        decryptedText.ShouldBe(originalText);
    }
}
