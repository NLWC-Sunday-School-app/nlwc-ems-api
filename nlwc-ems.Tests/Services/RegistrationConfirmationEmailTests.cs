using nlwc.ems.Core.Models;
using nlwc.ems.Services.Messaging;

namespace nlwc.ems.Tests.Services;

public class RegistrationConfirmationEmailTests
{
    private static readonly ConfirmationEdition Edition = new("Bible Bootcamp 2026", new DateOnly(2026, 12, 21), new DateOnly(2026, 12, 23),
        "Faith Plaza", "Igbogunu Bus Stop, Bariga, Lagos", Free: true, new TimeOnly(16, 0), new TimeOnly(18, 0));

    private static ConfirmationChild Child(string given, ParticipationMode mode = ParticipationMode.Day, string status = "Confirmed", bool pending = false, int age = 9)
        => new(given, $"{given} Egwu", age, $"BB26-{given.Length:0000}-A7", mode, status, pending);

    private static string Render(params ConfirmationChild[] children) => RegistrationConfirmationEmail.Render("Ada", Edition, children, "https://events.test/");

    [Fact]
    public void One_child_fills_the_template_and_leaves_no_merge_fields_behind()
    {
        var html = Render(Child("Tobi"));
        html.Should().Contain("Congratulations, Ada!").And.Contain("<strong>Tobi</strong>").And.Contain("We are so glad your child will be with us.")
            .And.Contain("Mon 21 – Wed 23 December 2026").And.Contain("Faith Plaza, Igbogunu Bus Stop, Bariga, Lagos").And.Contain(">Free<")
            .And.Contain("4:00pm – 6:00pm daily").And.Contain("Tobi Egwu").And.Contain("9 years old").And.Contain("Walk-in").And.Contain("Confirmed");
        html.Should().Contain("Tobi is registered for Bible Bootcamp 2026");
        html.Should().NotContain("{{").And.NotContain("CAMP_START").And.NotContain("Sample rows");
    }

    [Fact]
    public void Several_children_are_named_together_and_the_wording_turns_plural()
    {
        var html = Render(Child("Tobi"), Child("Dara"), Child("Ife"));
        html.Should().Contain("Tobi, Dara and Ife").And.Contain("We are so glad your children will be with us.").And.Contain("Tobi, Dara and Ife are registered");
        html.Should().Contain("Dara Egwu").And.Contain("Ife Egwu");
    }

    [Fact]
    public void The_camp_checklist_appears_only_when_a_child_asked_for_camp()
    {
        var walkIn = Render(Child("Tobi"));
        walkIn.Should().NotContain("What to bring to camp").And.NotContain("Please do not bring these to camp");
        walkIn.Should().Contain("Frequently asked questions");   // everything outside the camp block stays

        var camper = Render(Child("Dara", ParticipationMode.Camp, "Pending our team's review", pending: true, age: 12));
        camper.Should().Contain("What to bring to camp").And.Contain("Please do not bring these to camp").And.Contain("Pending our team&#39;s review").And.Contain("#B45309");
        camper.Should().Contain("Frequently asked questions");
    }

    [Fact]
    public void Names_and_references_cannot_inject_markup()
    {
        var html = Render(new ConfirmationChild("<script>x</script>", "<img src=x onerror=alert(1)> Egwu", 9, "BB\"><b>", ParticipationMode.Day, "Confirmed", false));
        html.Should().NotContain("<script>x</script>").And.NotContain("<img src=x").And.NotContain("BB\"><b>");
        html.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Links_and_the_logo_use_the_portal_address()
    {
        var html = Render(Child("Tobi"));
        html.Should().Contain("src=\"https://events.test/nlwc-logo-coloured.png\"").And.Contain("href=\"https://events.test/dashboard\"").And.Contain("href=\"https://events.test/privacy\"");
        html.Should().NotContain("../assets/");
    }

    [Fact]
    public void Missing_venue_pickup_and_a_paid_price_read_sensibly()
    {
        var html = RegistrationConfirmationEmail.Render("Ada", new ConfirmationEdition("E", new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 2), null, null, false, null, null), [Child("Tobi")], "https://x.test");
        html.Should().Contain("To be announced").And.Contain("See the event page").And.Contain("Wed 30 Dec 2026 – Sat 2 Jan 2027");
    }

    [Theory]
    [InlineData(new string[0], "")]
    [InlineData(new[] { "Tobi" }, "Tobi")]
    [InlineData(new[] { "Tobi", "Dara" }, "Tobi and Dara")]
    [InlineData(new[] { "Tobi", "Dara", "Ife" }, "Tobi, Dara and Ife")]
    public void Names_are_joined_the_way_a_person_would_write_them(string[] names, string expected)
        => RegistrationConfirmationEmail.JoinNames(names).Should().Be(expected);
}
