using nlwc.ems.Core;
using nlwc.ems.Core.Models;
using nlwc.ems.Dtos;

namespace nlwc.ems.Services.Implementations;

public class EditionService(IEditionDao editionDao, IRatePlanService ratePlanService, IAuditEventPublisher auditPublisher)
    : BaseService<EventEdition>(editionDao), IEditionService
{
    public async Task<ServiceResponse<EventEdition>> CreateAsync(EventEdition edition, CreateEditionRequest request)
    {
        var starts = request.StartsOn!.Value;
        var ends = request.EndsOn!.Value;
        if (ends < starts) return ServiceResponse.Fail<EventEdition>("The event can't end before it starts.");
        if (request.CampMinAge > request.CampMaxAge) return ServiceResponse.Fail<EventEdition>("The camp's minimum age can't exceed its maximum.");

        var template = await editionDao.RetrieveTemplateByCodeAsync(request.TemplateCode);
        if (template is null) return ServiceResponse.Fail<EventEdition>("Unknown event template.");
        if (await editionDao.SlugOrCodeExistsAsync(request.Slug, request.Code))
            throw new AppException("An edition with this slug or code already exists.", isConflict: true, code: "duplicate");

        edition.TemplateId = template.Id;
        edition.AgeReferenceDate = starts;   // age bands default to the first day of the event

        var venue = request.VenueName is null ? null : new Venue { Id = Guid.NewGuid(), Name = request.VenueName.Trim(), Address = request.VenueAddress?.Trim() };
        var days = new List<EventDay>();
        for (var d = starts; d <= ends; d = d.AddDays(1)) days.Add(new EventDay { Id = Guid.NewGuid(), EditionId = edition.Id, Day = d });

        // Every capability starts off; an edition opts in to what it needs (a free Bootcamp has no payment step).
        var capabilities = Enum.GetValues<Capability>()
            .Select(c => new EditionCapability { Id = Guid.NewGuid(), EditionId = edition.Id, Capability = c, Enabled = false }).ToList();

        await editionDao.InsertWithChildrenAsync(edition, venue, days, capabilities, new ReferenceCounter { Id = Guid.NewGuid(), EditionId = edition.Id });
        return ServiceResponse.Success((await editionDao.RetrieveByIdAsync(edition.Id))!);
    }

    public override Task<ServiceResponse<EventEdition>> PreUpdateAsync(EventEdition item)
        => Task.FromResult(item.CampMinAge > item.CampMaxAge
            ? ServiceResponse.Fail<EventEdition>("The camp's minimum age can't exceed its maximum.")
            : ServiceResponse.Success(item));

    public async Task<IReadOnlyList<PublicEditionDto>> ListPublicAsync()
    {
        var result = new List<PublicEditionDto>();
        foreach (var e in await editionDao.RetrievePublicAsync()) result.Add(await ToPublicAsync(e));
        return result;
    }

    public async Task<PublicEditionDto?> GetPublicAsync(string slug)
        => await editionDao.RetrievePublicBySlugAsync(slug) is { } e ? await ToPublicAsync(e) : null;

    private async Task<PublicEditionDto> ToPublicAsync(EventEdition e) => new()
    {
        Slug = e.Slug, Code = e.Code, Name = e.Name, Timezone = e.Timezone, StartsOn = e.StartsOn, EndsOn = e.EndsOn,
        Venue = e.Venue is null ? null : new PublicVenueDto { Name = e.Venue.Name, Address = e.Venue.Address },
        Camp = e.CampMinAge is null ? null : new PublicCampDto { MinAge = e.CampMinAge, MaxAge = e.CampMaxAge },
        PickupWindow = e.PickupWindowStart is { } s && e.PickupWindowEnd is { } t ? new PublicPickupWindowDto { Start = s, End = t } : null,
        Stage = e.Stage, RegistrationWindow = e.RegistrationWindow,
        Capabilities = e.Capabilities.Where(c => c.Enabled).Select(c => c.Capability).Order().ToList(),
        Price = new PublicPriceDto { State = await ratePlanService.GetPriceStateAsync(e.Id) },
        Days = e.Days.OrderBy(d => d.Day).Select(d => new PublicDayDto { Day = d.Day, Label = d.Label }).ToList()
    };

    public async Task SetCapabilityAsync(Guid editionId, Capability capability, bool enabled, string? reason)
    {
        if (!await editionDao.SetCapabilityAsync(editionId, capability, enabled)) throw new NotFoundException("Edition not found.");

        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(EventEdition), RecordId = editionId.ToString(), Action = "edition.capability", EditionId = editionId, Reason = reason,
            Detail = new { capability = capability.ToString(), enabled }
        });
    }

    public async Task TransitionAsync(Guid editionId, EditionStage to, string reason)
    {
        var edition = await editionDao.RetrieveByIdAsync(editionId) ?? throw new NotFoundException("Edition not found.");
        var from = edition.Stage ?? throw new AppException("The edition is in an unrecognised state.", isConflict: true);
        if (!EditionLifecycle.CanMove(from, to))
            throw new AppException($"An edition can't move from {from} to {to}.", isConflict: true, code: "invalid_transition");

        // An unconfigured price blocks opening: free has to be an explicit published rule.
        if (to == EditionStage.RegistrationOpen && await ratePlanService.GetPriceStateAsync(editionId) == PriceState.Pending)
            throw new AppException("Publish the edition's rates before opening registration.", isConflict: true, code: "pricing_not_published");

        var target = EditionLifecycle.Stages[to];
        edition.Visibility = target.Visibility;
        edition.RegistrationWindow = target.Registration;
        edition.LiveState = target.Live;
        await UpdateOrThrowAsync(edition);

        await auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(EventEdition), RecordId = editionId.ToString(), Action = "edition.stage", EditionId = editionId, Reason = reason,
            Detail = new { from = from.ToString(), to = to.ToString() }
        });
    }
}

public class RatePlanService(IRatePlanDao ratePlanDao, IRatePlanVersionDao versionDao, IAuditEventPublisher auditPublisher)
    : BaseService<RatePlan>(ratePlanDao), IRatePlanService
{
    public async Task<PriceState> GetPriceStateAsync(Guid editionId) => RateRules.StateOf(await ratePlanDao.RetrievePublishedKindsAsync(editionId));

    public async Task<Guid> DraftAsync(Guid actorAccountId, Guid editionId, string? note, IReadOnlyList<RateRuleInput> rules)
    {
        RateRules.Validate(rules);
        var entities = rules.Select(r => new RateRule
        {
            AgeMin = r.AgeMin, AgeMax = r.AgeMax, ParticipationMode = r.ParticipationMode, AccommodationCategory = r.AccommodationCategory,
            Kind = r.Kind, AmountKobo = r.AmountKobo
        }).ToList();

        var version = await versionDao.InsertDraftAsync(editionId, actorAccountId, note, entities) ?? throw new NotFoundException("Edition not found.");
        return version.Id;
    }

    public async Task ApproveAsync(Guid actorAccountId, Guid editionId, Guid versionId, string? note)
    {
        var version = await versionDao.RetrieveForEditionAsync(editionId, versionId) ?? throw new NotFoundException("Rate version not found.");
        if (version.State != RateVersionState.Draft) throw new AppException("Only a draft can be approved.", isConflict: true, code: "invalid_transition");
        // Separation of duties: the person who drafted a price cannot be the one who approves it.
        if (version.DraftedBy == actorAccountId) throw new ForbiddenException("You can't approve rates you drafted.", "separation_of_duties");

        version.State = RateVersionState.Approved;
        version.ApprovedBy = actorAccountId;
        version.ApprovedAt = DateTime.UtcNow;
        await versionDao.UpdateAsync(version);
        await PublishAuditAsync("rates.approved", editionId, versionId, actorAccountId, note);
    }

    public async Task PublishAsync(Guid actorAccountId, Guid editionId, Guid versionId, string? note)
    {
        var version = await versionDao.RetrieveForEditionAsync(editionId, versionId) ?? throw new NotFoundException("Rate version not found.");
        if (version.State != RateVersionState.Approved) throw new AppException("Only an approved version can be published.", isConflict: true, code: "invalid_transition");

        await versionDao.PublishAsync(version);
        await PublishAuditAsync("rates.published", editionId, versionId, actorAccountId, note);
    }

    private Task PublishAuditAsync(string action, Guid editionId, Guid versionId, Guid actor, string? note) =>
        auditPublisher.PublishAsync(new EntityAuditEvent
        {
            EntityType = nameof(RatePlanVersion), RecordId = versionId.ToString(), Action = action, EditionId = editionId, Reason = note, UserId = actor.ToString()
        });
}
