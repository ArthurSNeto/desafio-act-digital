using FluentAssertions;
using Library.IntegrationTests.Fixtures;
using System.Net;

namespace Library.IntegrationTests.Controllers;

[Collection("IntegrationTests")]
public class HealthChecksTests(CustomWebApplicationFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Live_Endpoint_Should_Return_Ok()
    {
        // Act
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Ready_Endpoint_Should_Return_Ok()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
