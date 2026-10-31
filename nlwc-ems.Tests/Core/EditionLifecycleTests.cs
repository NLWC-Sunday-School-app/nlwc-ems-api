using nlwc.ems.Core;

namespace nlwc.ems.Tests.Core;

public class EditionLifecycleTests
{
    [Fact]
    public void Every_stage_maps_to_a_distinct_triple_that_maps_back()
    {
        foreach (var (stage, fields) in EditionLifecycle.Stages)
            EditionLifecycle.StageOf(fields.Visibility, fields.Registration, fields.Live).Should().Be(stage);
        EditionLifecycle.Stages.Values.Distinct().Should().HaveCount(Enum.GetValues<EditionStage>().Length);
    }

    [Theory]
    [InlineData(EditionStage.Draft, EditionStage.Preview, true)]
    [InlineData(EditionStage.Preview, EditionStage.RegistrationOpen, true)]
    [InlineData(EditionStage.Closed, EditionStage.RegistrationOpen, true)]    // registration can reopen
    [InlineData(EditionStage.Draft, EditionStage.RegistrationOpen, false)]   // no skipping preview
    [InlineData(EditionStage.Draft, EditionStage.Live, false)]
    [InlineData(EditionStage.Live, EditionStage.RegistrationOpen, false)]    // no going back once live
    [InlineData(EditionStage.Archived, EditionStage.Draft, false)]
    public void Only_valid_moves_are_allowed(EditionStage from, EditionStage to, bool allowed) =>
        EditionLifecycle.CanMove(from, to).Should().Be(allowed);

    [Fact]
    public void Archived_is_terminal() =>
        Enum.GetValues<EditionStage>().Should().OnlyContain(to => !EditionLifecycle.CanMove(EditionStage.Archived, to));

    [Fact]
    public void An_unrecognised_combination_has_no_stage() =>
        EditionLifecycle.StageOf(EditionVisibility.Draft, RegistrationWindow.Open, EditionLiveState.Live).Should().BeNull();
}
