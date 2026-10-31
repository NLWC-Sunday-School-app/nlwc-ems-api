using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace nlwc.ems.Base.Web.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Trusts <c>X-Forwarded-For</c>/<c>X-Forwarded-Proto</c> so the real caller IP reaches the audit stamps. Trust
    /// defaults to loopback only; add the proxy's address to <c>KnownProxies</c> when one sits on another host.
    /// </summary>
    public static IApplicationBuilder UseEmsForwardedHeaders(this IApplicationBuilder app) =>
        app.UseForwardedHeaders(new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto });
}
