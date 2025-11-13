using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CVAnalyzer.IntegrationTests.Controllers;

public class HealthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Health_Returns_Success()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
