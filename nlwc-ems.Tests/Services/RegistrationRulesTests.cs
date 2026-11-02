using nlwc.ems.Core;
using nlwc.ems.Services.Implementations;

namespace nlwc.ems.Tests.Services;

public class RegistrationRulesTests
{
    [Theory]
    [InlineData(0, "under-3")]
    [InlineData(2, "under-3")]
    [InlineData(3, "3-9")]
    [InlineData(9, "3-9")]
    [InlineData(10, "10-16")]
    [InlineData(16, "10-16")]
    [InlineData(17, "17+")]
    [InlineData(45, "17+")]
    public void Age_bands_follow_the_boundaries(int age, string band) => RegistrationRules.AgeBand(age).Should().Be(band);

    [Theory]
    [InlineData(9, 10, 16, false)]
    [InlineData(10, 10, 16, true)]
    [InlineData(16, 10, 16, true)]
    [InlineData(17, 10, 16, false)]
    [InlineData(5, null, null, true)]
    [InlineData(5, 10, null, false)]
    public void Camp_ages_treat_a_null_bound_as_open(int age, int? min, int? max, bool allowed)
        => RegistrationRules.CampAgeAllowed(age, min, max).Should().Be(allowed);

    [Theory]
    [InlineData(20, 20, true)]
    [InlineData(21, 20, false)]
    [InlineData(32, 20, false)]
    [InlineData(32, null, true)]
    public void An_edition_age_limit_applies_to_everyone_and_null_means_no_limit(int age, int? max, bool allowed)
        => RegistrationRules.WithinEditionAge(age, max).Should().Be(allowed);

    [Fact]
    public void A_child_needs_three_consents_and_an_adult_only_the_terms()
    {
        RegistrationRules.RequiredConsents(9).Should().BeEquivalentTo([ConsentKind.Terms, ConsentKind.GuardianAuthorization, ConsentKind.MedicalEmergency]);
        RegistrationRules.RequiredConsents(18).Should().Equal(ConsentKind.Terms);
        RegistrationRules.RequiredConsents(17).Should().Contain(ConsentKind.GuardianAuthorization);
    }

    [Fact]
    public void Media_is_never_a_required_consent()
    {
        for (var age = 0; age < 90; age++) RegistrationRules.RequiredConsents(age).Should().NotContain(ConsentKind.Media);
    }

    [Theory]
    [InlineData(ParticipationMode.Day, true)]
    [InlineData(ParticipationMode.Camp, true)]
    [InlineData(ParticipationMode.Online, false)]
    public void Only_day_and_camp_are_physical(ParticipationMode mode, bool physical) => RegistrationRules.IsPhysical(mode).Should().Be(physical);

    [Fact]
    public void References_are_stable_padded_and_check_characters_catch_a_typo()
    {
        var reference = RegistrationReference.Format("BB26", 412);
        reference.Should().StartWith("BB26-0412-").And.MatchRegex(@"^BB26-0412-[A-HJ-NP-Z][0-9]$");
        RegistrationReference.Format("BB26", 412).Should().Be(reference);
        RegistrationReference.Format("BB26", 421).Should().NotBe(reference);
        RegistrationReference.Format("BB26", 12345).Should().StartWith("BB26-12345-");   // grows past four digits rather than truncating
    }

    [Fact]
    public void Health_none_must_stand_alone_and_unknown_conditions_are_refused()
    {
        HealthRules.Validate(true, null, null, null).Should().BeEmpty();
        HealthRules.Validate(false, [" Asthma ", "asthma"], null, null).Should().Equal("asthma");
        HealthRules.Validate(false, null, "peanuts", null).Should().BeEmpty();   // free text alone is an answer

        FluentActions.Invoking(() => HealthRules.Validate(true, ["fever"], null, null)).Should().Throw<ValidationException>().Which.Code.Should().Be("health_none_exclusive");
        FluentActions.Invoking(() => HealthRules.Validate(false, null, null, null)).Should().Throw<ValidationException>().Which.Code.Should().Be("health_unanswered");
        FluentActions.Invoking(() => HealthRules.Validate(false, ["flu"], null, null)).Should().Throw<ValidationException>().Which.Code.Should().Be("health_unknown_condition");
        FluentActions.Invoking(() => HealthRules.Validate(false, ["other"], null, null)).Should().Throw<ValidationException>().Which.Code.Should().Be("health_other_needs_notes");
    }
}
