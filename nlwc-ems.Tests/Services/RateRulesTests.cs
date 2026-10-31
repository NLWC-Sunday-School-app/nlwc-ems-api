using nlwc.ems.Dtos;
using nlwc.ems.Services.Implementations;

namespace nlwc.ems.Tests.Services;

public class RateRulesTests
{
    private static RateRuleInput Rule(int min, int? max, RateKind kind = RateKind.Free, long? amount = null, ParticipationMode? mode = null, string? category = null) =>
        new() { AgeMin = min, AgeMax = max, Kind = kind, AmountKobo = amount, ParticipationMode = mode, AccommodationCategory = category };

    [Fact]
    public void An_explicit_free_rule_is_valid()
    {
        var act = () => RateRules.Validate([Rule(3, 16), Rule(17, null)]);
        act.Should().NotThrow();
    }

    [Fact]
    public void No_rules_is_not_free()
    {
        var act = () => RateRules.Validate([]);
        act.Should().Throw<ValidationException>().WithMessage("*not the same as free*");
    }

    [Theory]
    [InlineData(RateKind.Amount, null)]    // an amount rule must carry an amount
    [InlineData(RateKind.Amount, 0L)]      // and it must be more than zero: zero is Free
    [InlineData(RateKind.Amount, -5L)]
    [InlineData(RateKind.Free, 500L)]      // a free rule carries no amount
    public void A_rule_must_agree_with_its_kind(RateKind kind, long? amount)
    {
        var act = () => RateRules.Validate([Rule(0, 12, kind, amount)]);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void A_range_must_not_run_backwards()
    {
        var act = () => RateRules.Validate([Rule(10, 5)]);
        act.Should().Throw<ValidationException>();
    }

    [Fact]
    public void Overlapping_ranges_for_the_same_mode_and_category_are_rejected()
    {
        var act = () => RateRules.Validate([Rule(3, 16), Rule(16, 20)]);
        act.Should().Throw<ValidationException>().Which.Code.Should().Be("overlapping_rules");
    }

    [Fact]
    public void An_open_ended_range_overlaps_anything_above_it()
    {
        var act = () => RateRules.Validate([Rule(17, null), Rule(30, 40)]);
        act.Should().Throw<ValidationException>().Which.Code.Should().Be("overlapping_rules");
    }

    [Fact]
    public void The_same_ages_may_be_priced_differently_per_mode_or_category()
    {
        var act = () => RateRules.Validate([
            Rule(3, 16, mode: ParticipationMode.Day), Rule(3, 16, RateKind.Amount, 500_000, ParticipationMode.Camp), Rule(3, 16, category: "hostel")]);
        act.Should().NotThrow();
    }

    [Fact]
    public void Price_state_is_free_only_when_every_published_rule_is_free()
    {
        RateRules.StateOf([]).Should().Be(PriceState.Pending);
        RateRules.StateOf([RateKind.Free, RateKind.Free]).Should().Be(PriceState.Free);
        RateRules.StateOf([RateKind.Free, RateKind.Amount]).Should().Be(PriceState.Amount);
    }
}
