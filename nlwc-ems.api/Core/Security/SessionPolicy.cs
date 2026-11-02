using nlwc.ems.Core.Models;

namespace nlwc.ems.Core.Security;

/// <summary>How long each kind of sign-in lasts. One place, so the server-side expiry and the browser cookie can't disagree.</summary>
public static class SessionPolicy
{
    /// <summary>A portal sign-in lasts a month from the moment of sign-in, however often or rarely the person visits.</summary>
    public static readonly TimeSpan WebLifetime = TimeSpan.FromDays(30);

    // Staff and gate sessions are short on purpose: a console left open, or a lost phone, should expire quickly.
    public static TimeSpan Idle(Surface s) => s switch { Surface.Admin => TimeSpan.FromMinutes(30), Surface.Checkin => TimeSpan.FromMinutes(15), _ => WebLifetime };

    public static TimeSpan Absolute(Surface s) => s == Surface.Web ? WebLifetime : TimeSpan.FromHours(12);

    /// <summary>How long the browser should keep the cookie. Null means a session cookie, which the browser drops when it closes: right for
    /// staff and gate devices, wrong for the portal, where it would sign families out whenever the browser restarts.</summary>
    public static TimeSpan? CookieLifetime(Surface s) => s == Surface.Web ? WebLifetime : null;
}
