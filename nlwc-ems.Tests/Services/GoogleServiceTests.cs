using System.Text;
using System.Text.Json;
using nlwc.ems.Core.Models;
using nlwc.ems.Services.Implementations;

namespace nlwc.ems.Tests.Services;

public class GoogleSignInServiceTests
{
    private readonly Mock<IAccountDao> _accounts = new();
    private readonly Mock<ISessionService> _sessions = new();
    private readonly GoogleSignInService _sut;
    private static readonly GoogleIdentity Ada = new("sub-1", "ada@example.com", true, "Ada", "Obi");

    public GoogleSignInServiceTests()
    {
        _sut = new GoogleSignInService(_accounts.Object, _sessions.Object);
        _sessions.Setup(s => s.CreateAsync(It.IsAny<Guid>(), Surface.Web)).ReturnsAsync(("tok", null!));
    }

    private static Account Acct(string? sub = null, BaseStatus status = BaseStatus.Active) => new() { Id = Guid.NewGuid(), Email = "ada@example.com", GoogleSub = sub, Status = status };

    [Fact]
    public async Task A_known_google_subject_signs_in_without_touching_anything_else()
    {
        var a = Acct("sub-1");
        _accounts.Setup(d => d.RetrieveByGoogleSubAsync("sub-1")).ReturnsAsync(a);
        await _sut.SignInAsync(Ada);
        _sessions.Verify(s => s.CreateAsync(a.Id, Surface.Web), Times.Once);
        _accounts.Verify(d => d.LinkGoogleAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task A_disabled_account_cannot_sign_in_with_google()
    {
        _accounts.Setup(d => d.RetrieveByGoogleSubAsync("sub-1")).ReturnsAsync(Acct("sub-1", BaseStatus.Inactive));
        await _sut.Invoking(s => s.SignInAsync(Ada)).Should().ThrowAsync<UnauthorizedException>();
        _sessions.Verify(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<Surface>()), Times.Never);
    }

    [Fact]
    public async Task An_unknown_person_gets_a_passwordless_account_with_a_verified_email()
    {
        await _sut.SignInAsync(Ada);
        _accounts.Verify(d => d.InsertGoogleAccountAsync(It.Is<Person>(p => p.GivenName == "Ada"),
            It.Is<Account>(a => a.PasswordHash == null && a.GoogleSub == "sub-1" && a.Email == "ada@example.com"),
            It.Is<VerifiedContact>(c => c.VerifiedAt != null && c.Value == "ada@example.com")), Times.Once);
    }

    [Fact]
    public async Task An_unknown_person_whose_email_google_has_not_verified_gets_nothing()
    {
        await _sut.Invoking(s => s.SignInAsync(Ada with { EmailVerified = false })).Should().ThrowAsync<UnauthorizedException>().WithMessage("*not verified*");
        _accounts.Verify(d => d.InsertGoogleAccountAsync(It.IsAny<Person>(), It.IsAny<Account>(), It.IsAny<VerifiedContact>()), Times.Never);
    }

    [Fact]
    public async Task An_existing_account_is_linked_when_both_sides_have_verified_the_email()
    {
        var a = Acct();
        _accounts.Setup(d => d.RetrieveByEmailAsync("ada@example.com")).ReturnsAsync(a);
        _accounts.Setup(d => d.HasVerifiedEmailAsync(a.Id)).ReturnsAsync(true);
        _accounts.Setup(d => d.LinkGoogleAsync(a.Id, "sub-1")).ReturnsAsync(true);
        await _sut.SignInAsync(Ada);
        _sessions.Verify(s => s.CreateAsync(a.Id, Surface.Web), Times.Once);
    }

    [Fact]
    public async Task An_existing_account_with_an_unconfirmed_email_is_not_linked()
    {
        var a = Acct();
        _accounts.Setup(d => d.RetrieveByEmailAsync("ada@example.com")).ReturnsAsync(a);
        _accounts.Setup(d => d.HasVerifiedEmailAsync(a.Id)).ReturnsAsync(false);
        var ex = await _sut.Invoking(s => s.SignInAsync(Ada)).Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("google_link_needs_password");
        _accounts.Verify(d => d.LinkGoogleAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task An_existing_account_is_not_linked_when_google_does_not_vouch_for_the_email()
    {
        _accounts.Setup(d => d.RetrieveByEmailAsync("ada@example.com")).ReturnsAsync(Acct());
        var ex = await _sut.Invoking(s => s.SignInAsync(Ada with { EmailVerified = false })).Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("google_email_unverified");
    }

    [Fact]
    public async Task An_account_already_linked_to_another_google_identity_is_not_taken_over()
    {
        var a = Acct(sub: "someone-else");
        _accounts.Setup(d => d.RetrieveByEmailAsync("ada@example.com")).ReturnsAsync(a);
        _accounts.Setup(d => d.HasVerifiedEmailAsync(a.Id)).ReturnsAsync(true);
        var ex = await _sut.Invoking(s => s.SignInAsync(Ada)).Should().ThrowAsync<UnauthorizedException>();
        ex.Which.Code.Should().Be("google_link_conflict");
    }
}

public class GoogleIdTokenTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);

    private static string Jwt(object payload)
    {
        static string B64(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return $"{B64(Encoding.UTF8.GetBytes("{}"))}.{B64(JsonSerializer.SerializeToUtf8Bytes(payload))}.sig";
    }

    private static Dictionary<string, object> Good() => new()
    {
        ["iss"] = "https://accounts.google.com", ["aud"] = "client", ["nonce"] = "n", ["exp"] = Now.ToUnixTimeSeconds() + 600,
        ["sub"] = "123", ["email"] = "Ada@Example.com", ["email_verified"] = true, ["given_name"] = "Ada", ["family_name"] = "Obi"
    };

    private static GoogleIdentity Read(Dictionary<string, object> c) => GoogleOidcClient.ReadClaims(Jwt(c), "client", "n", Now);

    [Fact]
    public void A_good_token_gives_a_lowercased_identity()
    {
        var id = Read(Good());
        id.Should().Be(new GoogleIdentity("123", "ada@example.com", true, "Ada", "Obi"));
    }

    [Theory]
    [InlineData("iss", "https://evil.example")]
    [InlineData("aud", "someone-elses-client")]
    [InlineData("nonce", "replayed")]
    [InlineData("exp", 1_799_999_999)]
    public void A_token_for_the_wrong_issuer_audience_nonce_or_time_is_rejected(string claim, object value)
    {
        var c = Good();
        c[claim] = value;
        var ex = Assert.Throws<UnauthorizedException>(() => Read(c));
        ex.Code.Should().Be("google_failed");
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("email")]
    public void A_token_without_a_subject_or_email_is_rejected(string claim)
    {
        var c = Good();
        c.Remove(claim);
        Assert.Throws<UnauthorizedException>(() => Read(c));
    }

    [Fact]
    public void Garbage_is_a_failed_sign_in_not_a_crash()
        => Assert.Throws<UnauthorizedException>(() => GoogleOidcClient.ReadClaims("not.a.jwt!", "client", "n", Now));

    [Fact]
    public void An_unverified_email_is_reported_as_such()
    {
        var c = Good();
        c["email_verified"] = false;
        Read(c).EmailVerified.Should().BeFalse();
    }
}
