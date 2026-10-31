using nlwc.ems.Dtos;

namespace nlwc.ems.Services.Implementations;

/// <summary>Validation of a rate-plan draft's rules, kept pure so it can be tested without a database.</summary>
public static class RateRules
{
    /// <summary>An empty rule set is not "free": free is an explicit rule, and a missing price blocks registration.</summary>
    public static void Validate(IReadOnlyList<RateRuleInput> rules)
    {
        if (rules.Count == 0) throw new ValidationException(["Add at least one rate rule. A missing price is not the same as free."]);

        foreach (var r in rules)
        {
            var ok = r.AgeMin >= 0 && (r.AgeMax is null || r.AgeMax >= r.AgeMin)
                && ((r.Kind == RateKind.Free && r.AmountKobo is null) || (r.Kind == RateKind.Amount && r.AmountKobo is > 0));
            if (!ok) throw new ValidationException(["A rule needs a valid age range, and an amount in kobo only when it is not free."]);
        }

        // No two rules may cover the same age for the same mode and category.
        foreach (var group in rules.GroupBy(r => (r.ParticipationMode, r.AccommodationCategory)))
        {
            var ordered = group.OrderBy(r => r.AgeMin).ToList();
            for (var i = 1; i < ordered.Count; i++)
                if (ordered[i - 1].AgeMax is null || ordered[i - 1].AgeMax >= ordered[i].AgeMin)
                    throw new ValidationException(["Rate rules overlap for the same mode and category."], "overlapping_rules");
        }
    }

    public static PriceState StateOf(IReadOnlyCollection<RateKind> publishedKinds) =>
        publishedKinds.Count == 0 ? PriceState.Pending : publishedKinds.All(k => k == RateKind.Free) ? PriceState.Free : PriceState.Amount;
}
