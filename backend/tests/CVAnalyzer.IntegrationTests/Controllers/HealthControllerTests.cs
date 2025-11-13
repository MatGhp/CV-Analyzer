using System.Net;
using Xunit;

namespace CVAnalyzer.IntegrationTests.Controllers;

public class HealthControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public HealthControllerTests(CustomWebApplicationFactory<Program> factory)
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
