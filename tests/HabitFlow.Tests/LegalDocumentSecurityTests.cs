using HabitFlow.Application;
using Xunit;

namespace HabitFlow.Tests;

public sealed class LegalDocumentSecurityTests
{
    [Fact]
    public void Sanitizer_keeps_allowlisted_markup_and_removes_executable_content()
    {
        var result = new LegalContentSanitizer().Sanitize(
            "<h2 onclick=\"steal()\">Resumo</h2><script>alert(1)</script><p>Seguro</p><iframe src=x>evil</iframe>");

        Assert.Equal("<h2>Resumo</h2><p>Seguro</p>", result);
        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,evil")]
    [InlineData("http://insecure.example")]
    public void Sanitizer_rejects_unsafe_link_protocols(string href)
    {
        var result = new LegalContentSanitizer().Sanitize($"<a href=\"{href}\">link</a>");
        Assert.Equal("<a>link</a>", result);
    }

    [Fact]
    public void Content_hash_is_stable_sha256_and_changes_with_content()
    {
        var service = new LegalContentHashService();
        var first = service.Compute("conteúdo sanitizado");
        Assert.Equal(64, first.Length);
        Assert.Equal(first, service.Compute("conteúdo sanitizado"));
        Assert.NotEqual(first, service.Compute("outro conteúdo"));
    }
}
