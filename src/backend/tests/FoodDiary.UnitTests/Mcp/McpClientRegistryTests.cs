using FoodDiary.API.Mcp;
using FoodDiary.API.Mcp.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace FoodDiary.UnitTests.Mcp;

public class McpClientRegistryTests
{
    private const string ChatGptCallback = "https://chatgpt.com/connector_platform_oauth_redirect";

    private readonly McpClientRegistry _registry = new(
        new EphemeralDataProtectionProvider(),
        TimeProvider.System,
        Options.Create(new McpOptions
        {
            Enabled = true,
            BaseUrl = "https://diary.example.com",
            Clients =
            [
                new McpClientRegistration
                {
                    ClientId = "claude", ClientSecret = "secret", RedirectUri = "https://claude.ai/api/mcp/auth_callback"
                }
            ]
        }));

    [Fact]
    public void DynamicClient_IsFoundWithWhatItRegistered()
    {
        var credentials = _registry.Register(new ClientMetadata("ChatGPT", [ChatGptCallback], TokenEndpointAuthMethods.None));

        var client = _registry.Find(credentials.Client.ClientId);

        client.Should().BeEquivalentTo(new RegisteredClient(
            credentials.Client.ClientId,
            "ChatGPT",
            [ChatGptCallback],
            TokenEndpointAuthMethods.None,
            IsDynamic: true));
    }

    [Fact]
    public void PublicDynamicClient_GetsNoSecret_AndAuthenticatesWithoutOne()
    {
        var credentials = _registry.Register(new ClientMetadata(null, [ChatGptCallback], TokenEndpointAuthMethods.None));

        credentials.ClientSecret.Should().BeNull();
        _registry.Authenticate(credentials.Client, presentedSecret: null).Should().BeTrue();
    }

    [Theory]
    [InlineData(TokenEndpointAuthMethods.ClientSecretPost)]
    [InlineData(TokenEndpointAuthMethods.ClientSecretBasic)]
    public void ConfidentialDynamicClient_AuthenticatesOnlyWithItsOwnSecret(string authMethod)
    {
        var credentials = _registry.Register(new ClientMetadata(null, [ChatGptCallback], authMethod));
        var otherCredentials = _registry.Register(new ClientMetadata(null, [ChatGptCallback], authMethod));

        _registry.Authenticate(credentials.Client, credentials.ClientSecret).Should().BeTrue();
        _registry.Authenticate(credentials.Client, otherCredentials.ClientSecret).Should().BeFalse();
        _registry.Authenticate(credentials.Client, "not-a-secret").Should().BeFalse();
        _registry.Authenticate(credentials.Client, presentedSecret: null).Should().BeFalse();
    }

    [Fact]
    public void ConfiguredClient_AuthenticatesOnlyWithConfiguredSecret()
    {
        var client = _registry.Find("claude")!;

        client.IsDynamic.Should().BeFalse();
        _registry.Authenticate(client, "secret").Should().BeTrue();
        _registry.Authenticate(client, "SECRET").Should().BeFalse();
        _registry.Authenticate(client, presentedSecret: null).Should().BeFalse();
    }

    [Theory]
    [InlineData("dcr.not-a-registration")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    public void UnknownClientId_IsNotFound(string? clientId)
    {
        _registry.Find(clientId).Should().BeNull();
    }
}
