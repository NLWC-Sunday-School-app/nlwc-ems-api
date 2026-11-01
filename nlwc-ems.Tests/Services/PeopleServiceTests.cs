using nlwc.ems.Base.Core.Audit;
using nlwc.ems.Core.Models;
using nlwc.ems.Dtos;
using nlwc.ems.Services.Implementations;

namespace nlwc.ems.Tests.Services;

public class PeopleServiceTests
{
    private readonly Mock<IGuardianAuthorizationDao> _guardians = new();
    private readonly Mock<IAuthorizedCollectorDao> _collectors = new();
    private readonly Mock<IAuditEventPublisher> _audit = new();
    private readonly Mock<IFieldEncryptor> _encryptor = new();
    private readonly Guid _guardian = Guid.NewGuid();
    private readonly Guid _child = Guid.NewGuid();

    public PeopleServiceTests()
    {
        _encryptor.Setup(e => e.EncryptString(It.IsAny<string>())).Returns<string>(s => System.Text.Encoding.UTF8.GetBytes("enc:" + s));
        _encryptor.Setup(e => e.DecryptString(It.IsAny<byte[]>())).Returns<byte[]>(b => System.Text.Encoding.UTF8.GetString(b)[4..]);
        _collectors.Setup(c => c.InsertAsync(It.IsAny<AuthorizedCollector>())).ReturnsAsync((AuthorizedCollector c) => c);
        _collectors.Setup(c => c.UpdateAsync(It.IsAny<AuthorizedCollector>())).ReturnsAsync((AuthorizedCollector c) => c);
        _guardians.Setup(g => g.UpdateAsync(It.IsAny<GuardianAuthorization>())).ReturnsAsync((GuardianAuthorization g) => g);
    }

    private void GuardianOf(Guid child) =>
        _guardians.Setup(g => g.RetrieveActiveAsync(_guardian, child)).ReturnsAsync(new GuardianAuthorization { Id = Guid.NewGuid(), GuardianPersonId = _guardian, DependantPersonId = child });

    private static string Dob(int ageYears) => DateTime.UtcNow.AddYears(-ageYears).AddDays(-30).ToString("yyyy-MM-dd");

    private DependantService Dependants() => new(_guardians.Object, _audit.Object);
    private CollectorService Collectors() => new(_collectors.Object, _guardians.Object, _encryptor.Object);

    // ---- dependants ----
    [Theory]
    [InlineData(0, true)]
    [InlineData(12, true)]
    [InlineData(13, true)]
    [InlineData(17, true)]
    [InlineData(18, false)]
    [InlineData(30, false)]
    public async Task Only_minors_up_to_seventeen_can_be_dependants(int age, bool allowed)
    {
        var act = () => Dependants().AddAsync(_guardian, new AddDependantRequest
        {
            GivenName = "Tobi", FamilyName = "Obi", DateOfBirth = DateOnly.Parse(Dob(age)), Relationship = GuardianRelationship.Parent
        });

        if (allowed) await act.Should().NotThrowAsync();
        else (await act.Should().ThrowAsync<ValidationException>()).Which.Code.Should().Be("too_old_for_dependant");
    }

    [Fact]
    public async Task A_dependant_is_created_with_the_guardians_authority_and_a_relationship_row()
    {
        Person? person = null; GuardianAuthorization? authorization = null; PersonRelationship? relationship = null;
        _guardians.Setup(g => g.InsertDependantAsync(It.IsAny<Person>(), It.IsAny<GuardianAuthorization>(), It.IsAny<PersonRelationship>()))
            .Callback((Person p, GuardianAuthorization a, PersonRelationship r) => { person = p; authorization = a; relationship = r; }).Returns(Task.CompletedTask);

        var id = await Dependants().AddAsync(_guardian, new AddDependantRequest
        {
            GivenName = " Tobi ", FamilyName = "Obi", DateOfBirth = DateOnly.Parse(Dob(8)), Relationship = GuardianRelationship.Grandparent
        });

        id.Should().Be(person!.Id);
        person.GivenName.Should().Be("Tobi");
        authorization!.GuardianPersonId.Should().Be(_guardian);
        authorization.DependantPersonId.Should().Be(id);
        authorization.DeclaredRelationship.Should().Be("Grandparent");
        relationship!.Kind.Should().Be(RelationshipKind.DependantOf);
    }

    [Fact]
    public async Task Removing_a_dependant_revokes_rather_than_deletes_and_records_why()
    {
        GuardianOf(_child);
        await Dependants().RemoveAsync(_guardian, _child, "added by mistake");

        _guardians.Verify(g => g.UpdateAsync(It.Is<GuardianAuthorization>(a => a.RevokedAt != null)), Times.Once);
        _guardians.Verify(g => g.RemoveAsync(It.IsAny<GuardianAuthorization>()), Times.Never);
        _audit.Verify(a => a.PublishAsync(It.Is<EntityAuditEvent>(x => x.Action == "dependant.removed" && x.Reason == "added by mistake")), Times.Once);
    }

    [Fact]
    public async Task Removing_someone_elses_child_is_not_found()
    {
        _guardians.Setup(g => g.RetrieveActiveAsync(_guardian, _child)).ReturnsAsync((GuardianAuthorization?)null);
        await Dependants().Invoking(s => s.RemoveAsync(_guardian, _child, "x")).Should().ThrowAsync<NotFoundException>();
        _audit.Verify(a => a.PublishAsync(It.IsAny<EntityAuditEvent>()), Times.Never);
    }

    // ---- collectors ----
    [Fact]
    public async Task A_collector_is_stored_with_an_encrypted_e164_phone()
    {
        GuardianOf(_child);
        AuthorizedCollector? saved = null;
        _collectors.Setup(c => c.InsertAsync(It.IsAny<AuthorizedCollector>())).Callback((AuthorizedCollector c) => saved = c).ReturnsAsync((AuthorizedCollector c) => c);

        await Collectors().AddAsync(_guardian, _child, new AddCollectorRequest { FullName = "Chidi Obi", Relationship = "uncle", Phone = "0803 123 4567" });

        System.Text.Encoding.UTF8.GetString(saved!.PhoneEncrypted).Should().Be("enc:+2348031234567");
        saved.ChildPersonId.Should().Be(_child);
        saved.ValidDays.Should().BeNull();   // every day
    }

    [Fact]
    public async Task A_collector_cannot_be_added_to_a_child_you_are_not_guardian_of()
    {
        _guardians.Setup(g => g.RetrieveActiveAsync(_guardian, _child)).ReturnsAsync((GuardianAuthorization?)null);
        await Collectors().Invoking(s => s.AddAsync(_guardian, _child, new AddCollectorRequest { FullName = "X", Relationship = "x", Phone = "08031234567" })).Should().ThrowAsync<NotFoundException>();
        _collectors.Verify(c => c.InsertAsync(It.IsAny<AuthorizedCollector>()), Times.Never);
    }

    [Fact]
    public async Task An_empty_day_list_is_refused_rather_than_meaning_never_or_always()
    {
        GuardianOf(_child);
        await Collectors().Invoking(s => s.AddAsync(_guardian, _child, new AddCollectorRequest { FullName = "X", Relationship = "x", Phone = "08031234567", ValidDays = [] })).Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Removing_a_collector_keeps_the_row_for_the_custody_record()
    {
        GuardianOf(_child);
        var c = new AuthorizedCollector { Id = Guid.NewGuid(), ChildPersonId = _child };
        _collectors.Setup(d => d.RetrieveByIdAsync(c.Id)).ReturnsAsync(c);

        await Collectors().RemoveAsync(_guardian, _child, c.Id);

        c.RemovedAt.Should().NotBeNull();
        _collectors.Verify(d => d.RemoveAsync(It.IsAny<AuthorizedCollector>()), Times.Never);
    }

    [Fact]
    public async Task A_collector_of_a_different_child_or_already_removed_is_not_found()
    {
        GuardianOf(_child);
        var other = new AuthorizedCollector { Id = Guid.NewGuid(), ChildPersonId = Guid.NewGuid() };
        var removed = new AuthorizedCollector { Id = Guid.NewGuid(), ChildPersonId = _child, RemovedAt = DateTime.UtcNow };
        _collectors.Setup(d => d.RetrieveByIdAsync(other.Id)).ReturnsAsync(other);
        _collectors.Setup(d => d.RetrieveByIdAsync(removed.Id)).ReturnsAsync(removed);

        await Collectors().Invoking(s => s.RemoveAsync(_guardian, _child, other.Id)).Should().ThrowAsync<NotFoundException>();
        await Collectors().Invoking(s => s.RemoveAsync(_guardian, _child, removed.Id)).Should().ThrowAsync<NotFoundException>();
    }
}
