using nlwc.ems.Core.Models;

namespace nlwc.ems.Services.Interfaces;

/// <summary>What Google vouches for after a successful sign-in.</summary>
public sealed record GoogleIdentity(string Sub, string Email, bool EmailVerified, string GivenName, string FamilyName);

/// <summary>The redirect to Google plus the secrets that must come back with the callback.</summary>
public sealed record GoogleAuthorization(string Url, string State, string Nonce, string CodeVerifier);

/// <summary>Talks to Google: builds the authorization URL (authorization code + PKCE) and trades the code for the person's identity.</summary>
public interface IGoogleOidcClient
{
    GoogleAuthorization Begin();

    /// <summary>Exchanges the code server-to-server and checks the ID token's issuer, audience, expiry and nonce. Throws
    /// <see cref="UnauthorizedException"/> (code <c>google_failed</c>) when anything is off.</summary>
    Task<GoogleIdentity> CompleteAsync(string code, string codeVerifier, string nonce);
}

public interface IGoogleSignInService
{
    /// <summary>Finds, links or creates the account for a Google identity and opens a web session.</summary>
    Task<(string Token, SessionPrincipal Principal)> SignInAsync(GoogleIdentity identity);
}
