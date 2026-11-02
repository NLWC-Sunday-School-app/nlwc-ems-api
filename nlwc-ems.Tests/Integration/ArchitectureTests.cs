using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using nlwc.ems.Base.Core.Security;
using nlwc.ems.Core.Security;

namespace nlwc.ems.Tests.Integration;

/// <summary>
/// Guards that unit tests cannot: routing and exposure only exist once the real host is built. Both come from real
/// incidents in the sibling services (ambiguous routes failing as a CORS-looking 500; a base controller exposing every
/// record to any signed-in user).
/// </summary>
[Collection("app")]
public sealed class ArchitectureTests(TestApp app)
{
    /// <summary>The only actions anyone may call without signing in. Adding to this list is a deliberate, reviewed act.</summary>
    private static readonly string[] AnonymousAllowList =
    [
        "POST api/v1/auth/signup", "POST api/v1/auth/verify-email", "POST api/v1/auth/login",
        "GET api/v1/auth/providers", "GET api/v1/auth/google/start", "GET api/v1/auth/google/callback",
        "GET api/v1/public/edition", "GET api/v1/public/edition/{slug}", "GET api/v1/public/coming-soon",
    ];

    private IReadOnlyList<ApiDescription> Actions() =>
        app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>().ApiDescriptionGroups.Items.SelectMany(g => g.Items).ToList();

    private static string Key(ApiDescription a) => $"{a.HttpMethod} {a.RelativePath}";

    [Fact]
    public void No_two_actions_share_a_method_and_path_ignoring_case()
    {
        var duplicates = Actions().GroupBy(a => (a.HttpMethod, Path: a.RelativePath!.ToLowerInvariant())).Where(g => g.Count() > 1).Select(g => $"{g.Key.HttpMethod} {g.Key.Path}");
        duplicates.Should().BeEmpty("ASP.NET routing ignores case, and an ambiguous match is a 500 raised before the exception middleware runs");
    }

    [Fact]
    public void Every_action_requires_sign_in_unless_it_is_on_the_anonymous_allow_list()
    {
        var open = Actions()
            .Where(a => a.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any() || !a.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
            .Select(a => Key(a).ToLowerInvariant()).Order().ToList();

        open.Should().Equal(AnonymousAllowList.Select(x => x.ToLowerInvariant()).Order());
    }

    [Fact]
    public void The_base_controllers_destructive_actions_are_hidden_where_they_do_not_belong()
    {
        // Editions, roles and grants are never deleted or toggled: they move through a lifecycle, are seeded, or are revoked.
        var paths = Actions().Select(a => Key(a).ToLowerInvariant()).ToList();
        paths.Should().NotContain(p => p.StartsWith("delete api/v1/edition/") || p.StartsWith("delete api/v1/role/") || p.StartsWith("delete api/v1/rolegrant/"));
        paths.Should().NotContain(p => p.Contains("/enable") || p.Contains("/disable"));
    }

    [Fact]
    public void Every_permission_code_used_by_an_attribute_exists_in_the_catalog()
    {
        var codes = Actions().SelectMany(a => a.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>())
            .Select(d => d.Policy).Where(p => p?.StartsWith(RequirePermissionAttribute.PolicyPrefix) == true)
            .Select(p => p![RequirePermissionAttribute.PolicyPrefix.Length..]).Distinct().ToList();

        codes.Should().NotBeEmpty();
        codes.Should().OnlyContain(c => EmsPermissions.Known(c.Split(':')[0], c.Split(':')[1]), "a typo would silently make an action unreachable");
    }

    [Fact]
    public void Every_controller_is_covered_by_the_scan()
    {
        var controllers = typeof(Program).Assembly.GetTypes().Where(t => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t) && !t.IsAbstract).ToList();
        var routed = Actions().Select(a => (a.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.AsType()).Where(t => t is not null).Distinct().ToList();
        routed.Should().BeEquivalentTo(controllers);
    }
}
