using mark.davison.common.abstractions.Services;
using mark.davison.common.authentication.server.Services;
using Moq;

namespace mark.davison.common.authentication.tests.Helpers;

public sealed class AuthTokenHelpersTests
{
    private readonly Mock<IRedisTicketStore> _redisTicketStoreMock;
    private readonly Mock<IDateService> _dateServiceMock;

    public AuthTokenHelpersTests()
    {
        _redisTicketStoreMock = new(MockBehavior.Strict);
        _dateServiceMock = new(MockBehavior.Strict);
    }

    [Test]
    public async Task NormalizeTokenTimes_DoesNothingForUtcTime()
    {
        var current = DateTime.UtcNow;

        var properties = new AuthenticationProperties();

        AuthenticationTokenExtensions.StoreTokens(
            properties,
            [
                new AuthenticationToken
                {
                    Name= AuthConstants.ExpiresAt,
                    Value = current.ToString("o")
                }
            ]
        );

        AuthTokenHelpers.NormalizeTokenTimes(properties);

        await Assert.That(properties.GetTokenValue(AuthConstants.ExpiresAt)).IsEqualTo(current.ToString("o"));
    }

    [Test]
    public async Task NormalizeTokenTimes_UpdatesLocalTime()
    {
        var current = DateTime.Now;

        var properties = new AuthenticationProperties();

        AuthenticationTokenExtensions.StoreTokens(
            properties,
            [
                new AuthenticationToken
                {
                    Name= AuthConstants.ExpiresAt,
                    Value = current.ToString("o")
                }
            ]
        );

        AuthTokenHelpers.NormalizeTokenTimes(properties);

        await Assert.That(properties.GetTokenValue(AuthConstants.ExpiresAt)).IsNotEqualTo(current.ToString("o"));
    }

    [Test]
    public async Task RefreshTokenIfNeeded_WhereExpirationTimeInvalid_ReturnsFalse()
    {

        var properties = new AuthenticationProperties();

        AuthenticationTokenExtensions.StoreTokens(
            properties,
            [
                new AuthenticationToken
                {
                    Name= AuthConstants.ExpiresAt,
                    Value = "not a date time"
                }
            ]
        );

        var now = DateTime.UtcNow;

        _dateServiceMock
            .Setup(_ => _.Now)
            .Returns(now);

        await Assert.
            That(
                AuthTokenHelpers.RefreshTokenIfNeeded(
                    _dateServiceMock.Object,
                    _redisTicketStoreMock.Object,
                    properties))
            .IsEqualTo(
                false);
    }

    [Test]
    public async Task RefreshTokenIfNeeded_WhereClientIdEmpty_ReturnsFalse()
    {

        var now = DateTime.UtcNow;

        var properties = new AuthenticationProperties();

        AuthenticationTokenExtensions.StoreTokens(
            properties,
            [
                new AuthenticationToken
                {
                    Name= AuthConstants.ExpiresAt,
                    Value = now.ToString("o")
                }
            ]
        );

        _dateServiceMock
            .Setup(_ => _.Now)
            .Returns(now);

        await Assert.
            That(
                AuthTokenHelpers.RefreshTokenIfNeeded(
                    _dateServiceMock.Object,
                    _redisTicketStoreMock.Object,
                    properties))
            .IsEqualTo(
                false);
    }

    [Test]
    public async Task RefreshTokenIfNeeded_WhereTokenNotNearExpiration_ReturnsFalse()
    {
        var now = DateTime.UtcNow;

        var properties = new AuthenticationProperties();

        AuthenticationTokenExtensions.StoreTokens(
            properties,
            [
                new AuthenticationToken
                {
                    Name= AuthConstants.ExpiresAt,
                    Value = now.AddSeconds(500).ToString("o")
                }
            ]
        );

        properties.Items.Add(AuthConstants.ClientId, "client_id");
        properties.Items.Add(AuthConstants.ClientSecret, "client_secret");
        properties.Items.Add(AuthConstants.TokenEndpoint, "token_endpoint");

        _dateServiceMock
            .Setup(_ => _.Now)
            .Returns(now);

        await Assert.
            That(
                AuthTokenHelpers.RefreshTokenIfNeeded(
                    _dateServiceMock.Object,
                    _redisTicketStoreMock.Object,
                    properties))
            .IsEqualTo(
                false);
    }

    [Test]
    public async Task RefreshTokenIfNeeded_WhereTokenNearExpiration_ReturnsTrueAndRefreshesTokens()
    {
        var now = DateTime.UtcNow;

        var properties = new AuthenticationProperties();

        const string clientId = "client_id";
        const string clientSecret = "client_secret";
        const string tokenEndpoint = "token_endpoint";

        AuthenticationTokenExtensions.StoreTokens(
            properties,
            [
                new AuthenticationToken
                {
                    Name= AuthConstants.ExpiresAt,
                    Value = now.ToString("o")
                }
            ]
        );

        properties.Items.Add(AuthConstants.ClientId, clientId);
        properties.Items.Add(AuthConstants.ClientSecret, clientSecret);
        properties.Items.Add(AuthConstants.TokenEndpoint, tokenEndpoint);

        _dateServiceMock
            .Setup(_ => _.Now)
            .Returns(now);

        _redisTicketStoreMock
            .Setup(_ => _.RefreshTokensAsync(
                It.IsAny<string>(),
                clientId,
                clientSecret,
                tokenEndpoint))
            .ReturnsAsync([]);

        await Assert.
            That(
                AuthTokenHelpers.RefreshTokenIfNeeded(
                    _dateServiceMock.Object,
                    _redisTicketStoreMock.Object,
                    properties))
            .IsEqualTo(
                true);
    }
}
