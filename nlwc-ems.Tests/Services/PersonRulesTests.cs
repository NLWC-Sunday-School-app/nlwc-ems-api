using nlwc.ems.Services.Implementations;

namespace nlwc.ems.Tests.Services;

public class PersonRulesTests
{
    [Theory]
    [InlineData("08031234567", "+2348031234567")]
    [InlineData("0803 123 4567", "+2348031234567")]
    [InlineData("0803-123-4567", "+2348031234567")]
    [InlineData("2348031234567", "+2348031234567")]
    [InlineData("+2348031234567", "+2348031234567")]
    [InlineData("+44 7911 123456", "+447911123456")]
    public void Phones_are_normalised_to_e164(string input, string expected) =>
        PersonRules.NormalizePhone(input).Should().Be(expected);

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("0803123456")]      // one digit short
    [InlineData("not a number")]
    [InlineData("+0123456789")]     // a country code cannot start with 0
    public void Bad_phones_are_rejected(string input)
    {
        var act = () => PersonRules.NormalizePhone(input);
        act.Should().Throw<ValidationException>().Which.Code.Should().Be("invalid_phone");
    }

    [Fact]
    public void Age_is_computed_on_the_reference_date()
    {
        PersonRules.AgeOn(new DateOnly(2017, 12, 22), new DateOnly(2026, 12, 21)).Should().Be(8);    // birthday is the day after
        PersonRules.AgeOn(new DateOnly(2016, 12, 21), new DateOnly(2026, 12, 21)).Should().Be(10);   // birthday on the day
        PersonRules.AgeOn(new DateOnly(2013, 12, 22), new DateOnly(2026, 12, 21)).Should().Be(12);   // a day short of 13
    }

    [Fact]
    public void A_leap_day_birthday_counts_from_28_february_in_a_non_leap_year()
    {
        // The documented convention (see AgeOn). Pinned so a change to it is a decision, not an accident.
        PersonRules.AgeOn(new DateOnly(2016, 2, 29), new DateOnly(2026, 2, 27)).Should().Be(9);
        PersonRules.AgeOn(new DateOnly(2016, 2, 29), new DateOnly(2026, 2, 28)).Should().Be(10);
    }

    [Theory]
    [InlineData(1899, 12, 31)]
    [InlineData(2999, 1, 1)]
    public void An_impossible_date_of_birth_is_rejected(int y, int m, int d)
    {
        var act = () => PersonRules.ValidateDateOfBirth(new DateOnly(y, m, d));
        act.Should().Throw<ValidationException>().Which.Code.Should().Be("invalid_date_of_birth");
    }

    [Fact]
    public async Task A_guardian_check_answers_not_found_for_a_stranger()
    {
        var dao = new Mock<IGuardianAuthorizationDao>();
        dao.Setup(d => d.RetrieveActiveAsync(It.IsAny<Guid>(), It.IsAny<Guid>())).ReturnsAsync((nlwc.ems.Core.Models.GuardianAuthorization?)null);
        var act = () => PersonRules.RequireGuardianAsync(dao.Object, Guid.NewGuid(), Guid.NewGuid());
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
