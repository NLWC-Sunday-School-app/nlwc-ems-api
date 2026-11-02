using System.Net;

namespace nlwc.ems.Tests.Integration;

[Collection("app")]
public sealed class EmailTestEndpointTests(TestApp app)
{
    private readonly Harness h = new(app);
    private const string Url = "/api/v1/admin/email/test";

    [Fact]
    public async Task Only_staff_who_manage_staff_can_use_the_email_test()
    {
        (await h.Send(HttpMethod.Post, Url, null, new { to = "a@example.com" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await h.Send(HttpMethod.Post, Url, (await h.NewUser()).Headers, new { to = "a@example.com" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await h.Send(HttpMethod.Post, Url, (await h.StaffWith("registration_officer")).Headers, new { to = "a@example.com" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task It_validates_the_address_and_says_plainly_when_mailgun_is_not_configured()
    {
        var admin = await h.StaffWith("super_admin");
        (await h.Send(HttpMethod.Post, Url, admin.Headers, new { to = "not-an-email" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var response = await h.Send(HttpMethod.Post, Url, admin.Headers, new { to = "someone@example.com" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await Harness.Data(response);
        data.GetProperty("configured").GetBoolean().Should().BeFalse();   // the test environment has no Mailgun keys
        data.GetProperty("sent").GetBoolean().Should().BeFalse();
        data.GetProperty("to").GetString().Should().Be("someone@example.com");
    }
}
