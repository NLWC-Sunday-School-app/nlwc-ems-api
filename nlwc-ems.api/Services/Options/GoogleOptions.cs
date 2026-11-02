namespace nlwc.ems.Services.Options;

/// <summary>Google sign-in. Off while <see cref="ClientId"/> is empty, so local development and CI need no credentials.</summary>
public class GoogleOptions
{
    public const string SectionName = "Google";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Where Google sends the person back: the API's <c>/api/v1/auth/google/callback</c> as the browser reaches it
    /// (through the web app's rewrite, so the cookies stay first-party). Must match the console exactly.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>The web app's origin. The person is sent back to <c>{WebBaseUrl}{next}</c> after sign-in.</summary>
    public string WebBaseUrl { get; set; } = string.Empty;

    public bool Enabled => ClientId.Length > 0 && ClientSecret.Length > 0 && RedirectUri.Length > 0 && WebBaseUrl.Length > 0;
}
