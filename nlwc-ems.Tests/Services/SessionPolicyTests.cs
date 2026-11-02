using nlwc.ems.Core.Models;
using nlwc.ems.Core.Security;

namespace nlwc.ems.Tests.Services;

public class SessionPolicyTests
{
    [Fact]
    public void A_portal_sign_in_lasts_thirty_days_whether_or_not_the_person_visits()
    {
        SessionPolicy.Idle(Surface.Web).Should().Be(TimeSpan.FromDays(30));
        SessionPolicy.Absolute(Surface.Web).Should().Be(TimeSpan.FromDays(30));
        SessionPolicy.CookieLifetime(Surface.Web).Should().Be(TimeSpan.FromDays(30));
    }

    [Fact]
    public void Staff_and_gate_sessions_stay_short_and_their_cookies_end_with_the_browser()
    {
        SessionPolicy.Idle(Surface.Admin).Should().Be(TimeSpan.FromMinutes(30));
        SessionPolicy.Idle(Surface.Checkin).Should().Be(TimeSpan.FromMinutes(15));
        SessionPolicy.Absolute(Surface.Admin).Should().Be(TimeSpan.FromHours(12));
        SessionPolicy.CookieLifetime(Surface.Admin).Should().BeNull();
        SessionPolicy.CookieLifetime(Surface.Checkin).Should().BeNull();
    }
}
