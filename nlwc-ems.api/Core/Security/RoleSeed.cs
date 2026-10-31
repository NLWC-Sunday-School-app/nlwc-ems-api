namespace nlwc.ems.Core.Security;

/// <summary>
/// The role matrix. A draft for NLWC to review: medical and finance are never bundled into a generic admin role, and a
/// test pins that. Ids are fixed so the seed is a stable migration, not a runtime surprise.
/// </summary>
public static class RoleSeed
{
    public static readonly DateTime SeededAt = new(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc);

    public sealed record RoleDef(Guid Id, string Key, string Name, (string Resource, string Level)[] Permissions);

    private static Guid RoleId(int n) => new($"0192f5a0-0000-7000-8000-0000000001{n:D2}");

    public static readonly IReadOnlyList<RoleDef> Roles =
    [
        new(RoleId(1), "super_admin", "Super Admin",
            [("config", "edit"), ("staff", "edit"), ("audit", "view"), ("registrations", "edit"), ("people", "edit"), ("custody", "view"), ("health", "flags")]),
        new(RoleId(2), "event_admin", "Event Admin",
            [("config", "edit"), ("registrations", "edit"), ("people", "edit"), ("pricing", "draft"), ("custody", "view"), ("health", "flags"), ("communications", "edit")]),
        new(RoleId(3), "finance_officer", "Finance Officer",
            [("money", "edit"), ("pricing", "approve"), ("audit", "view"), ("registrations", "view")]),
        new(RoleId(4), "checkin_officer", "Child Check-in Officer", [("custody", "act"), ("health", "flags")]),
        new(RoleId(5), "child_safety_officer", "Child Safety Officer", [("custody", "view"), ("health", "flags"), ("registrations", "view")]),
        new(RoleId(6), "accommodation_manager", "Accommodation Manager", [("registrations", "view"), ("config", "view")]),
        new(RoleId(7), "medical_welfare", "Medical / Welfare", [("health", "full"), ("registrations", "view")]),
        new(RoleId(8), "volunteer_usher", "Volunteer / Usher", [("custody", "scan")]),
        new(RoleId(9), "registration_officer", "Registration Officer", [("registrations", "edit"), ("people", "edit"), ("health", "flags")]),
        new(RoleId(10), "camp_coordinator", "Camp Coordinator", [("custody", "act"), ("health", "flags"), ("registrations", "view")]),
        new(RoleId(11), "communications_officer", "Communications Officer", [("communications", "edit"), ("registrations", "view")]),
        new(RoleId(12), "auditor", "Auditor", [("audit", "view"), ("registrations", "view"), ("money", "view")]),
    ];

    public static Guid PermissionId(Guid roleId, string resource) =>
        new(System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes($"{roleId}:{resource}")));
}
