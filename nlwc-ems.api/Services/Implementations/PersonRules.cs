using System.Text.RegularExpressions;

namespace nlwc.ems.Services.Implementations;

/// <summary>Pure rules for people and family data, kept out of the services so they can be tested on their own.</summary>
public static partial class PersonRules
{
    /// <summary>Dependants are guardian-managed minors with no account of their own: 0 to 17. A person aged 13 or over is still an
    /// independent person with their own registration and price, but until they are 18 a guardian registers for them and gives
    /// the consents (ADR-0009).</summary>
    public const int MaxDependantAge = 17;

    /// <summary>Whole years on <paramref name="on"/>. A 29 February birthday counts as 28 February in a non-leap year
    /// (the <see cref="DateOnly.AddYears"/> convention), so a leap-day child is a year older one day earlier than under the
    /// 1 March convention. It only matters when an age band boundary falls on that exact day.</summary>
    public static int AgeOn(DateOnly dob, DateOnly on) => on.Year - dob.Year - (on < dob.AddYears(on.Year - dob.Year) ? 1 : 0);

    public static DateOnly TodayLagos() =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Africa/Lagos"));

    /// <summary>A birth date must be a real, past date in the last 120 years or so.</summary>
    public static DateOnly ValidateDateOfBirth(DateOnly dob)
    {
        if (dob > TodayLagos() || dob.Year < 1900) throw new ValidationException(["Enter a valid date of birth."], "invalid_date_of_birth");
        return dob;
    }

    /// <summary>E.164. Accepts +234…, 234…, or a Nigerian national number starting with 0.</summary>
    public static string NormalizePhone(string? raw)
    {
        var s = Regex.Replace(raw ?? "", @"[\s\-().]", "");
        if (s.StartsWith('0') && s.Length == 11) s = "+234" + s[1..];
        else if (s.StartsWith("234") && s.Length == 13) s = "+" + s;
        if (!E164().IsMatch(s)) throw new ValidationException(["Enter a valid phone number."], "invalid_phone");
        return s;
    }

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164();

    /// <summary>Not-found rather than forbidden, so a guardian cannot probe for other people's children.</summary>
    public static async Task RequireGuardianAsync(IGuardianAuthorizationDao guardians, Guid guardianPersonId, Guid dependantPersonId)
    {
        if (await guardians.RetrieveActiveAsync(guardianPersonId, dependantPersonId) is null) throw new NotFoundException("Not found.");
    }
}
