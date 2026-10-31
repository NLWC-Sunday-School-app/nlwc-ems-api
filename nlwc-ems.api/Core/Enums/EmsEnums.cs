namespace nlwc.ems.Core.Enums;

public enum Gender { Male, Female }

public enum Surface { Web, Admin, Checkin }

public enum ContactKind { Email, Phone }

public enum ClaimState { ClaimInvited, ClaimVerified, ProfileCompleted, Expired, Declined, Disputed }

public enum RelationshipKind { Spouse, GuardianOf, DependantOf, CollectorFor }

/// <summary>Stored: the three separate lifecycle fields (visibility, registration window, live state).
/// <see cref="Models.EventEdition.Stage"/> maps them onto the admin-facing lifecycle.</summary>
public enum EditionVisibility { Draft, Preview, Public, Archived }

public enum RegistrationWindow { NotOpen, Open, Closed }

public enum EditionLiveState { NotLive, Live, Completed }

public enum Capability { Accommodation, DailyCheckin, CampReview, StudioRequests, ParentForum, PaidRegistration }

public enum ParticipationMode { Day, Camp, Online }

public enum RateVersionState { Draft, Approved, Published, Retired }

/// <summary>An explicit <see cref="Free"/> is a published rule; a missing rule is "unconfigured" and blocks checkout.</summary>
public enum RateKind { Amount, Free }

public enum RegistrationState
{
    Draft, ProvisionalPendingClaim, Submitted, NeedsAction, Confirmed, Completed, Cancelled, Rejected, Expired
}

public enum RegistrationRole { Registrant, Beneficiary, Guardian, Sponsor, Staff }

/// <summary>Stages an admin moves an edition through; each maps onto the three stored lifecycle fields.</summary>
public enum EditionStage { Draft, Preview, RegistrationOpen, Closed, Live, Completed, Archived }

/// <summary>The relationship a guardian declares when adding a dependant.</summary>
public enum GuardianRelationship { Parent, LegalGuardian, Grandparent, OtherRelative }

/// <summary>What the public sees of an edition's price: only an explicit published rule makes it "free".</summary>
public enum PriceState { Pending, Free, Amount }
