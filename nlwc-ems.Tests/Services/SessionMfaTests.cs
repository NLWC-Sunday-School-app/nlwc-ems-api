using Microsoft.Extensions.Options;
using nlwc.ems.Core.Models;
using nlwc.ems.Services.Implementations;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Tests.Services;

public class SessionMfaTests
{
    private static async Task<bool> Verified(bool requireMfa, DateTime? mfaVerifiedAt)
    {
        var sessions = new Mock<IAuthSessionDao>();
        sessions.Setup(s => s.RetrieveActiveAsync(It.IsAny<string>())).ReturnsAsync(new ResolvedSession
        {
            Session = new AuthSession { Id = Guid.NewGuid(), AccountId = Guid.NewGuid(), Surface = Surface.Admin, MfaVerifiedAt = mfaVerifiedAt },
            PersonId = Guid.NewGuid(), EmailVerified = true
        });
        var grants = new Mock<IRoleGrantService>();
        grants.Setup(g => g.GetPermissionCodesAsync(It.IsAny<Guid>())).ReturnsAsync([]);
        var tokens = new Mock<ITokenGenerator>();
        tokens.Setup(t => t.Hash(It.IsAny<string>())).Returns("h");
        var sut = new SessionService(sessions.Object, grants.Object, tokens.Object, Options.Create(new SecurityOptions { RequireMfa = requireMfa }));
        return (await sut.ResolveAsync("token"))!.MfaVerified;
    }

    [Fact]
    public async Task By_default_a_session_is_verified_only_after_the_authenticator_code() => (await Verified(true, null)).Should().BeFalse();

    [Fact]
    public async Task A_session_that_entered_its_code_is_verified() => (await Verified(true, DateTime.UtcNow)).Should().BeTrue();

    [Fact]
    public async Task With_the_requirement_switched_off_every_session_counts_as_verified() => (await Verified(false, null)).Should().BeTrue();

    [Fact]
    public void The_requirement_is_on_unless_someone_turns_it_off() => new SecurityOptions().RequireMfa.Should().BeTrue();
}
