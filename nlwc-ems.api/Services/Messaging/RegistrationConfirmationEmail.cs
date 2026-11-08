using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using nlwc.ems.Core.Models;

namespace nlwc.ems.Services.Messaging;

public record ConfirmationChild(string GivenName, string FullName, int? Age, string Reference, ParticipationMode Mode, string StatusLabel, bool StatusIsPending);

public record ConfirmationEdition(string Name, DateOnly StartsOn, DateOnly EndsOn, string? VenueName, string? VenueAddress, TimeOnly? PickupStart, TimeOnly? PickupEnd);

/// <summary>Fills the Bible Bootcamp registration-confirmation template (copied from documentation/design/email) for one parent or guardian.
/// Everything that varies is HTML-encoded here; the template's own wording (FAQ, camp checklist, closing) is the church's copy for Bootcamp 2026.</summary>
public static class RegistrationConfirmationEmail
{
    private const string BootcampTemplate = "Templates/registration-confirmation-bootcamp.html";

    public static string ModeLabel(ParticipationMode mode) => mode switch { ParticipationMode.Camp => "Camp", ParticipationMode.Online => "Online", _ => "Walk-in" };

    /// <summary>"Tobi", "Tobi and Dara", "Tobi, Dara and Ife".</summary>
    public static string JoinNames(IReadOnlyList<string> names) => names.Count switch
    {
        0 => "",
        1 => names[0],
        _ => string.Join(", ", names.Take(names.Count - 1)) + " and " + names[^1]
    };

    public static string FormatDates(DateOnly start, DateOnly end)
    {
        var c = CultureInfo.GetCultureInfo("en-GB");
        if (start == end) return start.ToString("ddd d MMMM yyyy", c);
        return start.Month == end.Month && start.Year == end.Year
            ? $"{start.ToString("ddd d", c)} – {end.ToString("ddd d MMMM yyyy", c)}"
            : start.Year == end.Year ? $"{start.ToString("ddd d MMM", c)} – {end.ToString("ddd d MMM yyyy", c)}"
            : $"{start.ToString("ddd d MMM yyyy", c)} – {end.ToString("ddd d MMM yyyy", c)}";
    }

    /// <summary>"4 – 6pm" for a window on the hour in one half of the day, otherwise "11am – 2pm" or "4:30pm – 6:15pm".</summary>
    public static string FormatWindow(TimeOnly start, TimeOnly end)
    {
        string Short(TimeOnly t) => t.Minute == 0 ? t.ToString("%h", CultureInfo.InvariantCulture) : t.ToString("h:mm", CultureInfo.InvariantCulture);
        string Mer(TimeOnly t) => t.Hour < 12 ? "am" : "pm";
        return Mer(start) == Mer(end) ? $"{Short(start)} – {Short(end)}{Mer(end)}" : $"{Short(start)}{Mer(start)} – {Short(end)}{Mer(end)}";
    }

    /// <summary>When walk-in children are collected. NLWC's announced hours apply until the edition sets its own window.</summary>
    private static readonly string DefaultPickupWindow = FormatWindow(new TimeOnly(16, 0), new TimeOnly(18, 0));

    public static string Subject(string editionName) => $"Registration confirmed: {editionName}";

    public static string Render(string guardianFirstName, ConfirmationEdition edition, IReadOnlyList<ConfirmationChild> children, string webBaseUrl, IReadOnlyList<string>? pickupPersons = null)
    {
        var html = LoadTemplate();
        var web = webBaseUrl.TrimEnd('/');
        var multiple = children.Count > 1;
        var hasCamper = children.Any(c => c.Mode == ParticipationMode.Camp);

        // The camp checklist and the "do not bring" box only matter when a child asked for camp.
        const string start = "<!--CAMP_START-->", end = "<!--CAMP_END-->";
        var from = html.IndexOf(start, StringComparison.Ordinal);
        var to = html.IndexOf(end, StringComparison.Ordinal);
        if (from >= 0 && to > from)
            html = hasCamper ? html.Remove(to, end.Length).Remove(from, start.Length) : html.Remove(from, to + end.Length - from);

        var venue = string.Join(", ", new[] { edition.VenueName, edition.VenueAddress }.Where(v => !string.IsNullOrWhiteSpace(v)));
        var values = new Dictionary<string, string>
        {
            ["guardian_first_name"] = Enc(guardianFirstName),
            ["children_names"] = Enc(JoinNames(children.Select(c => c.GivenName).ToList())),
            ["is_are"] = multiple ? "are" : "is",
            ["child_will"] = multiple ? "children will" : "child will",
            ["dates"] = Enc(FormatDates(edition.StartsOn, edition.EndsOn)),
            ["venue"] = Enc(venue.Length > 0 ? venue : "To be announced"),
            ["pickup"] = Enc($"{(edition.PickupStart is { } a && edition.PickupEnd is { } b ? FormatWindow(a, b) : DefaultPickupWindow)} daily for walk-in participants"),
            ["pickup_persons"] = pickupPersons is { Count: > 0 } ? Enc(string.Join(", ", pickupPersons)) : "None added yet. You can add them in your profile.",
            ["children_rows"] = Rows(children),
            ["logo_url"] = Enc($"{web}/nlwc-logo-coloured.png"),
            ["passes_url"] = Enc($"{web}/dashboard"),
            ["privacy_url"] = Enc($"{web}/privacy"),
        };
        foreach (var (key, value) in values) html = html.Replace("{{" + key + "}}", value);
        return html;
    }

    private static string Rows(IReadOnlyList<ConfirmationChild> children)
    {
        var sb = new StringBuilder();
        foreach (var c in children)
        {
            var age = c.Age is null ? "" : c.Age == 1 ? " · 1 year old" : $" · {c.Age} years old";
            sb.Append("<tr><td class=\"rule\" style=\"padding:12px 0;border-top:1px solid #E6DFD8;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" border=\"0\"><tr>")
              .Append("<td class=\"stack\" style=\"font:400 15px/22px Arial,sans-serif;color:#3F3A36;\"><strong class=\"t-head\" style=\"color:#17120F;\">").Append(Enc(c.FullName)).Append("</strong> ")
              .Append("<span class=\"t-muted\" style=\"color:#6B635C;\">").Append(Enc(age)).Append("</span><br>")
              .Append("<span class=\"t-muted\" style=\"font-size:13px;color:#6B635C;\">Reference ").Append(Enc(c.Reference)).Append("</span></td>")
              .Append("<td class=\"stack\" align=\"right\" style=\"font:600 13px/20px Arial,sans-serif;color:#17120F;\">").Append(Enc(ModeLabel(c.Mode))).Append("<br>")
              .Append("<span style=\"font-weight:400;color:").Append(c.StatusIsPending ? "#B45309" : "#6B635C").Append(";\">").Append(Enc(c.StatusLabel)).Append("</span></td>")
              .Append("</tr></table></td></tr>");
        }
        return sb.ToString();
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);

    private static string LoadTemplate()
    {
        using var stream = typeof(RegistrationConfirmationEmail).Assembly.GetManifestResourceStream(BootcampTemplate)
            ?? throw new InvalidOperationException($"The email template {BootcampTemplate} is not embedded in the API.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
