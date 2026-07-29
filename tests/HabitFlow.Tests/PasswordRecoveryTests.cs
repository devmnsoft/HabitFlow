using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class PasswordRecoveryTests
{
    [Fact]
    public void Token_has_256_bits_and_only_hash_is_deterministic()
    {
        var service = new PasswordResetTokenService();
        var first = service.Create();
        var second = service.Create();
        Assert.NotEqual(first.RawToken, second.RawToken);
        Assert.True(first.RawToken.Length >= 43);
        Assert.Equal(64, first.Hash.Length);
        Assert.Equal(first.Hash, PasswordResetTokenService.Hash(first.RawToken));
        Assert.DoesNotContain(first.RawToken, first.Hash);
    }

    [Fact]
    public void Templates_include_text_html_and_no_password()
    {
        var message = TransactionalEmailService.PasswordReset("Maria Silva", "https://habitflow.example/reset-password?token=test", 30);
        Assert.Equal("Crie uma nova senha para o HabitFlow", message.Subject);
        Assert.Contains("Maria", message.Text);
        Assert.Contains("CRIAR NOVA SENHA", message.Html);
        Assert.DoesNotContain("senha atual:", message.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(" Person+alias@Example.com ", "person+alias@example.com")]
    public void Email_normalization_preserves_alias(string input, string expected) =>
        Assert.Equal(expected, PasswordRecoveryService.NormalizeEmail(input));
}
