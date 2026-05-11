using DigitalCards.Web;
using Microsoft.AspNetCore.Http;

namespace DigitalCards.Web.Tests;

public sealed class EnrollmentBaseUrlResolverTests
{
    [Fact]
    public void Resolve_UsesConfiguredPublicBaseUrlWhenPresent()
    {
        var baseUrl = EnrollmentBaseUrlResolver.Resolve(
            "https://algo.trycloudflare.com/",
            "http",
            new HostString("localhost:5088"));

        Assert.Equal("https://algo.trycloudflare.com", baseUrl);
    }

    [Fact]
    public void Resolve_FallsBackToRequestOrigin()
    {
        var baseUrl = EnrollmentBaseUrlResolver.Resolve(
            null,
            "http",
            new HostString("localhost:5088"));

        Assert.Equal("http://localhost:5088", baseUrl);
    }
}
