namespace nlwc.ems.Core.Security;

/// <summary>The browser origins we trust. A header alone is not proof of anything, but a request that names an origin we
/// do not know on a state-changing call is refused.</summary>
public class OriginPolicy(string[] allowed)
{
    public bool Allows(string? origin) => !string.IsNullOrEmpty(origin) && allowed.Contains(origin, StringComparer.OrdinalIgnoreCase);
}
