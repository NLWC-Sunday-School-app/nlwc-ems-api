namespace nlwc.ems.Services.Implementations;

/// <summary>Pure registration rules, kept out of the service so they can be tested on their own.</summary>
public static class RegistrationRules
{
    public const int AdultAge = 18;

    /// <summary>The age bands used for media wording and reports. 3–9 matches the legacy media release, 10–16 the camp range.</summary>
    public static string AgeBand(int age) => age switch { < 3 => "under-3", <= 9 => "3-9", <= 16 => "10-16", _ => "17+" };

    public static bool IsMinor(int age) => age < AdultAge;

    /// <summary>A null bound means unbounded. Only camp has an age range; day and online attendance are not age-gated.</summary>
    public static bool CampAgeAllowed(int age, int? min, int? max) => (min is null || age >= min) && (max is null || age <= max);

    /// <summary>Day and camp attendance are physical, so they need an emergency contact and health answers.</summary>
    public static bool IsPhysical(ParticipationMode mode) => mode is ParticipationMode.Day or ParticipationMode.Camp;

    /// <summary>The consents a registration must have accepted before it can be submitted. Media is never required.</summary>
    public static IReadOnlyList<ConsentKind> RequiredConsents(int age) =>
        IsMinor(age) ? [ConsentKind.Terms, ConsentKind.GuardianAuthorization, ConsentKind.MedicalEmergency] : [ConsentKind.Terms];

    public static bool Editable(RegistrationState state) => state is RegistrationState.Draft or RegistrationState.NeedsAction;

    public static bool Cancellable(RegistrationState state) =>
        state is RegistrationState.Draft or RegistrationState.ProvisionalPendingClaim or RegistrationState.Submitted
            or RegistrationState.NeedsAction or RegistrationState.Confirmed;
}

/// <summary>The disclosure form's vocabulary and its one hard rule: "none" is exclusive.</summary>
public static class HealthRules
{
    public static readonly IReadOnlySet<string> KnownConditions = new HashSet<string>
    {
        "fatigue", "dry_cough", "fever", "loss_of_taste_smell", "headache", "sore_throat", "nausea", "breathing_difficulty",
        "blocked_nose", "asthma", "ulcer", "sickle_cell", "other"
    };

    /// <summary>The cleaned condition list. Throws when the answer is missing, contradictory or uses an unknown condition.</summary>
    public static List<string> Validate(bool none, IReadOnlyCollection<string>? conditions, string? allergies, string? notes)
    {
        var cleaned = (conditions ?? []).Select(c => c.Trim().ToLowerInvariant()).Where(c => c.Length > 0).Distinct().ToList();
        if (none && cleaned.Count > 0) throw new ValidationException(["\"None\" cannot be combined with other conditions."], "health_none_exclusive");
        if (!none && cleaned.Count == 0 && string.IsNullOrWhiteSpace(allergies) && string.IsNullOrWhiteSpace(notes))
            throw new ValidationException(["Choose \"None\", or tell us the conditions, allergies or care notes."], "health_unanswered");
        var unknown = cleaned.Where(c => !KnownConditions.Contains(c)).ToList();
        if (unknown.Count > 0) throw new ValidationException([$"Unknown condition: {string.Join(", ", unknown)}."], "health_unknown_condition");
        if (cleaned.Contains("other") && string.IsNullOrWhiteSpace(notes)) throw new ValidationException(["Describe the other condition in the notes."], "health_other_needs_notes");
        return cleaned;
    }
}
