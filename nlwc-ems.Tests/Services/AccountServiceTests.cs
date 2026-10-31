using Microsoft.EntityFrameworkCore;
using nlwc.ems.Core.Models;
using nlwc.ems.Dtos;
using nlwc.ems.Services.Implementations;

namespace nlwc.ems.Tests.Services;

public class AccountServiceTests
{
    private readonly Mock<IAccountDao> _accounts = new();
    private readonly Mock<IEmailVerificationDao> _verifications = new();
    private readonly Mock<ISessionService> _sessions = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ITokenGenerator> _tokens = new();
    private readonly Mock<IMailer> _mailer = new();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _sut = new AccountService(_accounts.Object, _verifications.Object, _sessions.Object, _hasher.Object, _tokens.Object, _mailer.Object);
        _hasher.Setup(h => h.DummyHash).Returns("dummy");
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns("hashed");
        _tokens.Setup(t => t.NewToken()).Returns("tok");
        _tokens.Setup(t => t.Hash(It.IsAny<string>())).Returns<string>(x => "h:" + x);
    }

    private static SignUpRequest SignUp(string email = "Ada@Example.com") => new() { Email = email, Password = "correct horse battery", GivenName = "Ada", FamilyName = "Obi" };

    private Account Existing(string? hash = "stored", DateTime? lockedUntil = null, BaseStatus status = BaseStatus.Active)
    {
        var a = new Account { Id = Guid.NewGuid(), Email = "ada@example.com", PasswordHash = hash, LockedUntil = lockedUntil, Status = status };
        _accounts.Setup(d => d.RetrieveByEmailAsync("ada@example.com")).ReturnsAsync(a);
        return a;
    }

    // ---- sign-up ----
    [Fact]
    public async Task A_new_email_creates_the_account_and_sends_a_verification_link()
    {
        await _sut.SignUpAsync(SignUp());

        _accounts.Verify(d => d.InsertWithPersonAsync(It.IsAny<Person>(), It.Is<Account>(a => a.Email == "ada@example.com" && a.PasswordHash == "hashed"),
            It.Is<VerifiedContact>(c => c.Kind == ContactKind.Email && c.VerifiedAt == null), It.Is<EmailVerification>(v => v.TokenHash == "h:tok")), Times.Once);
        _mailer.Verify(m => m.SendAsync("ada@example.com", "verify_email", It.Is<IReadOnlyDictionary<string, string>>(d => d["token"] == "tok")), Times.Once);
    }

    [Fact]
    public async Task An_existing_email_creates_nothing_and_never_receives_a_token()
    {
        _accounts.Setup(d => d.EmailExistsAsync("ada@example.com")).ReturnsAsync(true);
        await _sut.SignUpAsync(SignUp());

        _accounts.Verify(d => d.InsertWithPersonAsync(It.IsAny<Person>(), It.IsAny<Account>(), It.IsAny<VerifiedContact>(), It.IsAny<EmailVerification>()), Times.Never);
        _mailer.Verify(m => m.SendAsync("ada@example.com", "account_exists", It.Is<IReadOnlyDictionary<string, string>>(d => d.Count == 0)), Times.Once);
    }

    [Fact]
    public async Task Both_paths_hash_the_password_so_they_cost_the_same()
    {
        _accounts.Setup(d => d.EmailExistsAsync("ada@example.com")).ReturnsAsync(true);
        await _sut.SignUpAsync(SignUp());
        _hasher.Verify(h => h.Hash("correct horse battery"), Times.Once);
    }

    [Fact]
    public async Task Losing_a_sign_up_race_is_treated_as_an_existing_account()
    {
        _accounts.Setup(d => d.InsertWithPersonAsync(It.IsAny<Person>(), It.IsAny<Account>(), It.IsAny<VerifiedContact>(), It.IsAny<EmailVerification>()))
            .ThrowsAsync(new DbUpdateException("duplicate"));
        await _sut.SignUpAsync(SignUp());
        _mailer.Verify(m => m.SendAsync("ada@example.com", "account_exists", It.IsAny<IReadOnlyDictionary<string, string>>()), Times.Once);
    }

    // ---- verification ----
    [Fact]
    public async Task A_used_or_unknown_verification_link_is_refused()
    {
        _verifications.Setup(v => v.ConsumeAsync("h:tok")).ReturnsAsync(false);
        (await _sut.Invoking(s => s.VerifyEmailAsync("tok")).Should().ThrowAsync<AppException>()).Which.Code.Should().Be("invalid_token");
    }

    // ---- login ----
    [Fact]
    public async Task An_unknown_email_and_a_wrong_password_fail_identically_and_both_verify_a_hash()
    {
        _accounts.Setup(d => d.RetrieveByEmailAsync("nobody@example.com")).ReturnsAsync((Account?)null);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        Existing();

        var unknown = (await _sut.Invoking(s => s.LogInAsync("nobody@example.com", "pw", Surface.Web)).Should().ThrowAsync<UnauthorizedException>()).Which;
        var wrong = (await _sut.Invoking(s => s.LogInAsync("ada@example.com", "pw", Surface.Web)).Should().ThrowAsync<UnauthorizedException>()).Which;

        unknown.Message.Should().Be(wrong.Message);
        unknown.Code.Should().Be(wrong.Code).And.Be("invalid_credentials");
        _hasher.Verify(h => h.Verify("dummy", "pw"), Times.Once);       // the timing equaliser for the unknown account
        _hasher.Verify(h => h.Verify("stored", "pw"), Times.Once);
    }

    [Fact]
    public async Task A_wrong_password_counts_as_a_failed_attempt()
    {
        var a = Existing();
        _hasher.Setup(h => h.Verify("stored", "pw")).Returns(false);
        await _sut.Invoking(s => s.LogInAsync("ada@example.com", "pw", Surface.Web)).Should().ThrowAsync<UnauthorizedException>();
        _accounts.Verify(d => d.RegisterFailedLoginAsync(a.Id, 5, It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task A_locked_account_is_refused_even_with_the_right_password_and_is_not_counted_again()
    {
        Existing(lockedUntil: DateTime.UtcNow.AddMinutes(10));
        _hasher.Setup(h => h.Verify("stored", "right")).Returns(true);

        (await _sut.Invoking(s => s.LogInAsync("ada@example.com", "right", Surface.Web)).Should().ThrowAsync<UnauthorizedException>()).Which.Code.Should().Be("locked");
        _accounts.Verify(d => d.RegisterFailedLoginAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>()), Times.Never);
        _sessions.Verify(s => s.CreateAsync(It.IsAny<Guid>(), It.IsAny<Surface>()), Times.Never);
    }

    [Fact]
    public async Task A_lock_that_has_expired_no_longer_blocks()
    {
        var a = Existing(lockedUntil: DateTime.UtcNow.AddMinutes(-1));
        _hasher.Setup(h => h.Verify("stored", "right")).Returns(true);
        _sessions.Setup(s => s.CreateAsync(a.Id, Surface.Web)).ReturnsAsync(("t", null!));
        await _sut.Invoking(s => s.LogInAsync("ada@example.com", "right", Surface.Web)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task A_disabled_account_cannot_sign_in_even_with_the_right_password()
    {
        Existing(status: BaseStatus.Inactive);
        _hasher.Setup(h => h.Verify("stored", "right")).Returns(true);
        await _sut.Invoking(s => s.LogInAsync("ada@example.com", "right", Surface.Web)).Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task A_google_only_account_has_no_password_to_sign_in_with()
    {
        Existing(hash: null);
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        await _sut.Invoking(s => s.LogInAsync("ada@example.com", "anything", Surface.Web)).Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task A_good_login_clears_the_failure_count_and_opens_a_session_on_the_requested_surface()
    {
        var a = Existing();
        _hasher.Setup(h => h.Verify("stored", "right")).Returns(true);
        _sessions.Setup(s => s.CreateAsync(a.Id, Surface.Admin)).ReturnsAsync(("cookie-token", null!));

        var (token, _) = await _sut.LogInAsync("  Ada@Example.com ", "right", Surface.Admin);

        token.Should().Be("cookie-token");
        _accounts.Verify(d => d.ResetFailedLoginsAsync(a.Id), Times.Once);
    }
}
