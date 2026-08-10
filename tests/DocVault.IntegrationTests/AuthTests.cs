using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace DocVault.IntegrationTests;

public class AuthTests
{
    private readonly HttpClient _userClient;

    public AuthTests()
    {
        _userClient = new HttpClient { BaseAddress = new System.Uri("http://localhost:5261") };
    }

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsToken()
    {
        var loginResp = await _userClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@docvault.com",
            password = "Admin@DocVault#2024"
        });

        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var login = await loginResp.Content.ReadFromJsonAsync<dynamic>();
        string token = login.token ?? login.Token ?? string.Empty;
        token.Should().NotBeNullOrEmpty();
    }
}
