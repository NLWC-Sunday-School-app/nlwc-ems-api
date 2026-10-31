using nlwc.ems.Base.Core.Audit;
using nlwc.ems.Core;
using nlwc.ems.Core.Models;
using nlwc.ems.Dtos;
using nlwc.ems.Services.Implementations;

namespace nlwc.ems.Tests.Services;

public class EditionServiceTests
{
    private readonly Mock<IEditionDao> _dao = new();
    private readonly Mock<IRatePlanService> _rates = new();
    private readonly Mock<IAuditEventPublisher> _audit = new();
    private readonly EditionService _sut;

    public EditionServiceTests()
    {
        _sut = new EditionService(_dao.Object, _rates.Object, _audit.Object);
        _dao.Setup(d => d.UpdateAsync(It.IsAny<EventEdition>())).ReturnsAsync((EventEdition e) => e);
    }

    private static EventEdition Edition(EditionStage stage)
    {
        var f = EditionLifecycle.Stages[stage];
        return new EventEdition { Id = Guid.NewGuid(), Visibility = f.Visibility, RegistrationWindow = f.Registration, LiveState = f.Live };
    }

    private EventEdition Given(EditionStage stage, PriceState price = PriceState.Free)
    {
        var e = Edition(stage);
        _dao.Setup(d => d.RetrieveByIdAsync(e.Id)).ReturnsAsync(e);
        _rates.Setup(r => r.GetPriceStateAsync(e.Id)).ReturnsAsync(price);
        return e;
    }

    private static CreateEditionRequest Request(string template = "BB", string start = "2026-12-21", string end = "2026-12-23") => new()
    {
        TemplateCode = template, Slug = "bb-2026", Code = "BB26", Name = "Bible Bootcamp", StartsOn = DateOnly.Parse(start), EndsOn = DateOnly.Parse(end)
    };

    [Fact]
    public async Task A_valid_move_updates_the_three_fields_and_records_why()
    {
        var e = Given(EditionStage.Draft);
        await _sut.TransitionAsync(e.Id, EditionStage.Preview, "ready for review");

        e.Stage.Should().Be(EditionStage.Preview);
        _audit.Verify(a => a.PublishAsync(It.Is<EntityAuditEvent>(x => x.Action == "edition.stage" && x.Reason == "ready for review" && x.EditionId == e.Id)), Times.Once);
    }

    [Fact]
    public async Task An_invalid_move_is_a_conflict_and_changes_nothing()
    {
        var e = Given(EditionStage.Draft);
        var act = () => _sut.TransitionAsync(e.Id, EditionStage.Live, "skip ahead");

        (await act.Should().ThrowAsync<AppException>()).Which.Should().Match<AppException>(x => x.IsConflict && x.Code == "invalid_transition");
        _dao.Verify(d => d.UpdateAsync(It.IsAny<EventEdition>()), Times.Never);
        _audit.Verify(a => a.PublishAsync(It.IsAny<EntityAuditEvent>()), Times.Never);
    }

    [Theory]
    [InlineData(PriceState.Pending, false)]    // an unconfigured price blocks opening
    [InlineData(PriceState.Free, true)]
    [InlineData(PriceState.Amount, true)]
    public async Task Registration_opens_only_with_a_published_price(PriceState price, bool allowed)
    {
        var e = Given(EditionStage.Preview, price);
        var act = () => _sut.TransitionAsync(e.Id, EditionStage.RegistrationOpen, "go");

        if (allowed) await act.Should().NotThrowAsync();
        else (await act.Should().ThrowAsync<AppException>()).Which.Code.Should().Be("pricing_not_published");
    }

    [Fact]
    public async Task Moving_a_missing_edition_is_not_found()
    {
        _dao.Setup(d => d.RetrieveByIdAsync(It.IsAny<Guid>())).ReturnsAsync((EventEdition?)null);
        await _sut.Invoking(s => s.TransitionAsync(Guid.NewGuid(), EditionStage.Preview, "x")).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Switching_a_capability_on_a_missing_edition_is_not_found_and_not_audited()
    {
        _dao.Setup(d => d.SetCapabilityAsync(It.IsAny<Guid>(), It.IsAny<Capability>(), It.IsAny<bool>())).ReturnsAsync(false);
        await _sut.Invoking(s => s.SetCapabilityAsync(Guid.NewGuid(), Capability.DailyCheckin, true, null)).Should().ThrowAsync<NotFoundException>();
        _audit.Verify(a => a.PublishAsync(It.IsAny<EntityAuditEvent>()), Times.Never);
    }

    [Fact]
    public async Task Switching_a_capability_is_audited_with_its_reason()
    {
        var id = Guid.NewGuid();
        _dao.Setup(d => d.SetCapabilityAsync(id, Capability.CampReview, true)).ReturnsAsync(true);
        await _sut.SetCapabilityAsync(id, Capability.CampReview, true, "camp is on");
        _audit.Verify(a => a.PublishAsync(It.Is<EntityAuditEvent>(x => x.Action == "edition.capability" && x.Reason == "camp is on")), Times.Once);
    }

    [Fact]
    public async Task Creating_with_an_unknown_template_fails_without_touching_the_database()
    {
        _dao.Setup(d => d.RetrieveTemplateByCodeAsync("ZZ")).ReturnsAsync((EventTemplate?)null);
        var result = await _sut.CreateAsync(new EventEdition { Id = Guid.NewGuid() }, Request("ZZ"));

        result.IsSuccessful.Should().BeFalse();
        _dao.Verify(d => d.InsertWithChildrenAsync(It.IsAny<EventEdition>(), It.IsAny<Venue?>(), It.IsAny<IReadOnlyList<EventDay>>(), It.IsAny<IReadOnlyList<EditionCapability>>(), It.IsAny<ReferenceCounter>()), Times.Never);
    }

    [Fact]
    public async Task Creating_an_edition_that_ends_before_it_starts_fails()
    {
        var result = await _sut.CreateAsync(new EventEdition { Id = Guid.NewGuid() }, Request(start: "2026-12-23", end: "2026-12-21"));
        result.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task A_duplicate_slug_or_code_is_a_conflict()
    {
        _dao.Setup(d => d.RetrieveTemplateByCodeAsync("BB")).ReturnsAsync(new EventTemplate { Id = Guid.NewGuid() });
        _dao.Setup(d => d.SlugOrCodeExistsAsync("bb-2026", "BB26")).ReturnsAsync(true);
        var act = () => _sut.CreateAsync(new EventEdition { Id = Guid.NewGuid() }, Request());
        (await act.Should().ThrowAsync<AppException>()).Which.IsConflict.Should().BeTrue();
    }

    [Fact]
    public async Task A_new_edition_gets_a_day_per_date_every_capability_off_and_an_age_reference_date()
    {
        var edition = new EventEdition { Id = Guid.NewGuid() };
        IReadOnlyList<EventDay>? days = null;
        IReadOnlyList<EditionCapability>? capabilities = null;
        _dao.Setup(d => d.RetrieveTemplateByCodeAsync("BB")).ReturnsAsync(new EventTemplate { Id = Guid.NewGuid() });
        _dao.Setup(d => d.InsertWithChildrenAsync(edition, It.IsAny<Venue?>(), It.IsAny<IReadOnlyList<EventDay>>(), It.IsAny<IReadOnlyList<EditionCapability>>(), It.IsAny<ReferenceCounter>()))
            .Callback((EventEdition _, Venue? _, IReadOnlyList<EventDay> d, IReadOnlyList<EditionCapability> c, ReferenceCounter _) => { days = d; capabilities = c; })
            .Returns(Task.CompletedTask);
        _dao.Setup(d => d.RetrieveByIdAsync(edition.Id)).ReturnsAsync(edition);

        var result = await _sut.CreateAsync(edition, Request());

        result.IsSuccessful.Should().BeTrue();
        days!.Select(d => d.Day).Should().Equal(new DateOnly(2026, 12, 21), new DateOnly(2026, 12, 22), new DateOnly(2026, 12, 23));
        capabilities!.Should().HaveCount(Enum.GetValues<Capability>().Length).And.OnlyContain(c => !c.Enabled);
        edition.AgeReferenceDate.Should().Be(new DateOnly(2026, 12, 21));
    }

    [Fact]
    public async Task The_public_view_marks_free_only_from_the_price_state_and_hides_internals()
    {
        var e = Edition(EditionStage.RegistrationOpen);
        e.Slug = "bb"; e.Name = "BB"; e.Venue = new Venue { Name = "Faith Plaza" }; e.CampMinAge = 10; e.CampMaxAge = 16; e.CampCapacity = 120;
        e.Capabilities = [new EditionCapability { Capability = Capability.CampReview, Enabled = true }, new EditionCapability { Capability = Capability.PaidRegistration, Enabled = false }];
        _dao.Setup(d => d.RetrievePublicBySlugAsync("bb")).ReturnsAsync(e);
        _rates.Setup(r => r.GetPriceStateAsync(e.Id)).ReturnsAsync(PriceState.Free);

        var dto = await _sut.GetPublicAsync("bb");

        dto!.Price.State.Should().Be(PriceState.Free);
        dto.Capabilities.Should().Equal(Capability.CampReview);
        dto.Camp.Should().BeEquivalentTo(new PublicCampDto { MinAge = 10, MaxAge = 16 });   // no capacity
    }

    [Fact]
    public async Task An_unknown_or_non_public_slug_is_null()
    {
        _dao.Setup(d => d.RetrievePublicBySlugAsync("nope")).ReturnsAsync((EventEdition?)null);
        (await _sut.GetPublicAsync("nope")).Should().BeNull();
    }
}
