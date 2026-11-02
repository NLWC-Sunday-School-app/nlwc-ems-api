using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using nlwc.ems.Core.Models;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Services.Implementations;

public class GoogleOidcClient(IHttpClientFactory httpClients, IOptions<GoogleOptions> options) : IGoogleOidcClient
{
    private const string AuthorizeEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private static readonly string[] Issuers = ["https://accounts.google.com", "accounts.google.com"];

    private static UnauthorizedException Failed() => new("Google sign-in could not be completed.", "google_failed");

    private static string Random() => Base64Url(RandomNumberGenerator.GetBytes(32));
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public GoogleAuthorization Begin()
    {
        var o = options.Value;
        string state = Random(), nonce = Random(), verifier = Random();
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var query = new Dictionary<string, string>
        {
            ["client_id"] = o.ClientId, ["redirect_uri"] = o.RedirectUri, ["response_type"] = "code", ["scope"] = "openid email profile",
            ["state"] = state, ["nonce"] = nonce, ["code_challenge"] = challenge, ["code_challenge_method"] = "S256", ["prompt"] = "select_account"
        };
        var url = AuthorizeEndpoint + "?" + string.Join("&", query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        return new GoogleAuthorization(url, state, nonce, verifier);
    }

    public async Task<GoogleIdentity> CompleteAsync(string code, string codeVerifier, string nonce)
    {
        var o = options.Value;
        using var response = await httpClients.CreateClient().PostAsync(TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code, ["client_id"] = o.ClientId, ["client_secret"] = o.ClientSecret, ["redirect_uri"] = o.RedirectUri,
            ["grant_type"] = "authorization_code", ["code_verifier"] = codeVerifier
        }));
        if (!response.IsSuccessStatusCode) throw Failed();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        if (!body.RootElement.TryGetProperty("id_token", out var idToken) || idToken.GetString() is not { } jwt) throw Failed();

        // The token came straight from Google's token endpoint over TLS, which OpenID Connect Core 3.1.3.7 accepts in place of
        // checking the signature. Everything else about it is still checked.
        return ReadClaims(jwt, o.ClientId, nonce, DateTimeOffset.UtcNow);
    }

    /// <summary>Reads and checks the ID token's claims. Public for tests; the signature is not checked here (see <see cref="CompleteAsync"/>).</summary>
    public static GoogleIdentity ReadClaims(string jwt, string clientId, string nonce, DateTimeOffset now)
    {
        try
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3) throw Failed();
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
            var c = doc.RootElement;

            string? Str(string name) => c.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
            var aud = c.TryGetProperty("aud", out var a) ? (a.ValueKind == JsonValueKind.Array ? a.EnumerateArray().Select(x => x.GetString()).ToArray() : [a.GetString()]) : [];
            var verified = c.TryGetProperty("email_verified", out var ev) && (ev.ValueKind == JsonValueKind.True || (ev.ValueKind == JsonValueKind.String && ev.GetString() == "true"));

            if (!Issuers.Contains(Str("iss")) || !aud.Contains(clientId) || Str("nonce") != nonce) throw Failed();
            if (!c.TryGetProperty("exp", out var exp) || exp.GetInt64() <= now.ToUnixTimeSeconds()) throw Failed();
            if (Str("sub") is not { Length: > 0 } sub || Str("email") is not { Length: > 0 } email) throw Failed();

            var given = Str("given_name") ?? Str("name") ?? email.Split('@')[0];
            return new GoogleIdentity(sub, email.Trim().ToLowerInvariant(), verified, given.Trim(), (Str("family_name") ?? "").Trim());
        }
        catch (Exception e) when (e is FormatException or JsonException or InvalidOperationException)
        {
            throw Failed();
        }
    }
}

public class GoogleSignInService(IAccountDao accountDao, ISessionService sessionService) : IGoogleSignInService
{
    public async Task<(string Token, SessionPrincipal Principal)> SignInAsync(GoogleIdentity identity)
    {
        var account = await accountDao.RetrieveByGoogleSubAsync(identity.Sub);

        if (account is null)
        {
            account = await accountDao.RetrieveByEmailAsync(identity.Email);
            if (account is null)
            {
                account = await CreateAsync(identity);
            }
            else
            {
                // Never link on a name or an unproven address: Google must vouch for the email AND the existing account's email must
                // already be confirmed, so nobody can take over an account by registering its address at Google.
                if (!identity.EmailVerified) throw new UnauthorizedException("Google has not verified this email address.", "google_email_unverified");
                if (!await accountDao.HasVerifiedEmailAsync(account.Id))
                    throw new UnauthorizedException("An account with this email exists. Sign in with your password first.", "google_link_needs_password");
                if (account.GoogleSub is not null || !await accountDao.LinkGoogleAsync(account.Id, identity.Sub))
                    throw new UnauthorizedException("This email is already linked to a different Google account.", "google_link_conflict");
            }
        }

        if (account.Status != BaseStatus.Active) throw new UnauthorizedException("This account is disabled.", "google_failed");
        return await sessionService.CreateAsync(account.Id, Surface.Web);
    }

    private async Task<Account> CreateAsync(GoogleIdentity identity)
    {
        if (!identity.EmailVerified) throw new UnauthorizedException("Google has not verified this email address.", "google_email_unverified");

        var person = new Person { Id = Guid.NewGuid(), GivenName = identity.GivenName, FamilyName = identity.FamilyName };
        var account = new Account { Id = Guid.NewGuid(), PersonId = person.Id, Email = identity.Email, GoogleSub = identity.Sub };
        // Google has confirmed the address, so there is no verification email to send.
        var contact = new VerifiedContact { Id = Guid.NewGuid(), PersonId = person.Id, Kind = ContactKind.Email, Value = identity.Email, VerifiedAt = DateTime.UtcNow };
        await accountDao.InsertGoogleAccountAsync(person, account, contact);
        return account;
    }
}
