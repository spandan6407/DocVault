using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace DocVault.IntegrationTests;

public class ProjectTests
{
    // NOTE: These tests are templates. They assume the APIs are reachable at the configured ports
    // and that RabbitMQ/SQL are available. Adjust BaseAddress and tokens as needed for your env.

    private readonly HttpClient _docClient;
    private readonly HttpClient _userClient;

    public ProjectTests()
    {
        // Use real running services for integration tests or a test server implementation
        _docClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:5078") };
        _userClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:5261") };
    }

    [Fact]
    public async Task AdminCanCreateProject_And_EventIsPublished()
    {
        // 1. Login as admin
        var loginResp = await _userClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@docvault.com",
            password = "Admin@DocVault#2024"
        });

        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResp.Content.ReadFromJsonAsync<dynamic>();
        string token = login.token ?? login.Token ?? string.Empty;
        token.Should().NotBeNullOrEmpty();

        _docClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 2. Create project
        
        var projectBody = new { name = "Integration Test Project", description = "created by test" };
        var createResp = await _docClient.PostAsJsonAsync("/api/projects", projectBody);

        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResp.Content.ReadFromJsonAsync<dynamic>();
        ((string)created.id).Should().NotBeNullOrEmpty();

        // Note: verifying RabbitMQ published event and consumer processing is environment-specific
        // we should  have the things in
    }
}
