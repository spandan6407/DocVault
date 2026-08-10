using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace DocVault.IntegrationTests;

public class UserTests
{
    private readonly HttpClient _userClient;

    public UserTests()
    {
        _userClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:5261") };
    }

    [Fact]
    
    public async Task AdminCanCreateUser_InProject()
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

        _userClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // 2. Create user in an existing project (manually ensure a project exists or create via document service before running test)
        var newUser = new
        {
            firstName = "Test",
            lastName = "User",
            email = $"testuser+{System.Guid.NewGuid():N}@docvault.com",
            password = "User@DocVault#2024",
            projectId = System.Guid.Empty // replace with a valid project id in env before running 
        };

        var createResp = await _userClient.PostAsJsonAsync("/api/users", newUser);

        // Depending on environment, this might be 200 or 400 if project not found; test asserts success in a fully configured env
        createResp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }
    // Additional tests can be added for user retrieval, update, and deletion as needed.
}
