using nlwc.ems.Base.Core.Audit;
using nlwc.ems.Core.Models;
using nlwc.ems.Dtos;
using Microsoft.Extensions.Options;
using nlwc.ems.Services.Implementations;
using nlwc.ems.Services.Options;

namespace nlwc.ems.Tests.Services;

public class RatePlanServiceTests
{
    private readonly Mock<IRatePlanDao> _plans = new();
    private readonly Mock<IRatePlanVersionDao> _versions = new();
    private readonly Mock<IAuditEventPublisher> _audit = new();
    private readonly RatePlanService _sut;
    private readonly Guid _edition = Guid.NewGuid();
    private readonly Guid _drafter = Guid.NewGuid();
    private readonly Guid _approver = Guid.NewGuid();

    private static RatePlanService Service(IRatePlanDao plans, IRatePlanVersionDao versions, IAuditEventPublisher audit, bool separation)
        => new(plans, versions, audit, Options.Create(new SecurityOptions { RequireSeparationOfDuties = separation }));

    public RatePlanServiceTests() => _sut = Service(_plans.Object, _versions.Object, _audit.Object, separation: true);

    private RatePlanVersion Version(RateVersionState state)
    {
        var v = new RatePlanVersion { Id = Guid.NewGuid(), State = state, DraftedBy = _drafter };
        _versions.Setup(d => d.RetrieveForEditionAsync(_edition, v.Id)).ReturnsAsync(v);
        return v;
    }

    [Fact]
    public async Task The_drafter_cannot_approve_their_own_rates()
    {
        var v = Version(RateVersionState.Draft);
        var act = () => _sut.ApproveAsync(_drafter, _edition, v.Id, null);

        (await act.Should().ThrowAsync<ForbiddenException>()).Which.Code.Should().Be("separation_of_duties");
        _versions.Verify(d => d.UpdateAsync(It.IsAny<RatePlanVersion>()), Times.Never);
    }

    [Fact]
    public async Task With_the_rule_off_the_drafter_can_approve_their_own_rates()
    {
        var v = Version(RateVersionState.Draft);
        var sut = Service(_plans.Object, _versions.Object, _audit.Object, separation: false);

        await sut.ApproveAsync(_drafter, _edition, v.Id, null);

        v.State.Should().Be(RateVersionState.Approved);
        v.ApprovedBy.Should().Be(_drafter);
    }

    [Fact]
    public async Task Another_person_can_approve_and_it_is_recorded()
    {
        var v = Version(RateVersionState.Draft);
        _versions.Setup(d => d.UpdateAsync(v)).ReturnsAsync(v);
        await _sut.ApproveAsync(_approver, _edition, v.Id, "looks right");

        v.State.Should().Be(RateVersionState.Approved);
        v.ApprovedBy.Should().Be(_approver);
        v.ApprovedAt.Should().NotBeNull();
        _audit.Verify(a => a.PublishAsync(It.Is<EntityAuditEvent>(x => x.Action == "rates.approved" && x.Reason == "looks right" && x.UserId == _approver.ToString())), Times.Once);
    }

    [Theory]
    [InlineData(RateVersionState.Approved)]
    [InlineData(RateVersionState.Published)]
    [InlineData(RateVersionState.Retired)]
    public async Task Only_a_draft_can_be_approved(RateVersionState state)
    {
        var v = Version(state);
        (await _sut.Invoking(s => s.ApproveAsync(_approver, _edition, v.Id, null)).Should().ThrowAsync<AppException>()).Which.IsConflict.Should().BeTrue();
    }

    [Theory]
    [InlineData(RateVersionState.Draft, false)]      // not approved yet
    [InlineData(RateVersionState.Approved, true)]
    [InlineData(RateVersionState.Published, false)]  // already live
    [InlineData(RateVersionState.Retired, false)]
    public async Task Only_an_approved_version_can_be_published(RateVersionState state, bool allowed)
    {
        var v = Version(state);
        var act = () => _sut.PublishAsync(_approver, _edition, v.Id, null);

        if (allowed)
        {
            await act.Should().NotThrowAsync();
            _versions.Verify(d => d.PublishAsync(v), Times.Once);
            _audit.Verify(a => a.PublishAsync(It.Is<EntityAuditEvent>(x => x.Action == "rates.published")), Times.Once);
        }
        else
        {
            await act.Should().ThrowAsync<AppException>();
            _versions.Verify(d => d.PublishAsync(It.IsAny<RatePlanVersion>()), Times.Never);
        }
    }

    [Fact]
    public async Task A_version_from_another_edition_is_not_found()
    {
        _versions.Setup(d => d.RetrieveForEditionAsync(_edition, It.IsAny<Guid>())).ReturnsAsync((RatePlanVersion?)null);
        await _sut.Invoking(s => s.ApproveAsync(_approver, _edition, Guid.NewGuid(), null)).Should().ThrowAsync<NotFoundException>();
        await _sut.Invoking(s => s.PublishAsync(_approver, _edition, Guid.NewGuid(), null)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Drafting_for_a_missing_edition_is_not_found()
    {
        _versions.Setup(d => d.InsertDraftAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<RateRule>>())).ReturnsAsync((RatePlanVersion?)null);
        await _sut.Invoking(s => s.DraftAsync(_drafter, _edition, null, [new RateRuleInput { AgeMin = 0, Kind = RateKind.Free }])).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Invalid_rules_never_reach_the_database()
    {
        await _sut.Invoking(s => s.DraftAsync(_drafter, _edition, null, [])).Should().ThrowAsync<ValidationException>();
        _versions.Verify(d => d.InsertDraftAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<IReadOnlyList<RateRule>>()), Times.Never);
    }

    [Fact]
    public async Task A_draft_carries_the_drafter_and_the_rules_as_given()
    {
        IReadOnlyList<RateRule>? saved = null;
        _versions.Setup(d => d.InsertDraftAsync(_edition, _drafter, "free 2026", It.IsAny<IReadOnlyList<RateRule>>()))
            .Callback((Guid _, Guid _, string? _, IReadOnlyList<RateRule> r) => saved = r)
            .ReturnsAsync(new RatePlanVersion { Id = Guid.NewGuid() });

        await _sut.DraftAsync(_drafter, _edition, "free 2026", [new RateRuleInput { AgeMin = 3, AgeMax = 16, Kind = RateKind.Free }, new RateRuleInput { AgeMin = 17, Kind = RateKind.Free }]);

        saved.Should().HaveCount(2).And.OnlyContain(r => r.Kind == RateKind.Free && r.AmountKobo == null);
    }

    [Theory]
    [InlineData(new RateKind[0], PriceState.Pending)]
    [InlineData(new[] { RateKind.Free }, PriceState.Free)]
    [InlineData(new[] { RateKind.Free, RateKind.Amount }, PriceState.Amount)]
    public async Task The_price_state_comes_from_the_published_rules(RateKind[] kinds, PriceState expected)
    {
        _plans.Setup(p => p.RetrievePublishedKindsAsync(_edition)).ReturnsAsync(kinds.ToList());
        (await _sut.GetPriceStateAsync(_edition)).Should().Be(expected);
    }
}
