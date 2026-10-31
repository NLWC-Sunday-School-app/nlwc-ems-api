using System.Reflection;
using System.Text.Json.Serialization;

namespace nlwc.ems.Base.Core.Audit;

public static class AuditFieldExtractor
{
    public static string? ResolveDisplayName(BaseEntity entity)
    {
        var prop = entity.GetType().GetProperties().FirstOrDefault(p => p.IsDefined(typeof(AuditDisplayNameAttribute)));
        return prop?.GetValue(entity)?.ToString();
    }

    /// <summary>Scalar properties only (no navigations), minus <see cref="TrailableIgnoreAttribute"/> and
    /// <see cref="JsonIgnoreAttribute"/> ones. When a property is shadowed with <c>new</c> the most-derived
    /// declaration wins, so the dictionary never sees the same key twice.</summary>
    public static IReadOnlyDictionary<string, object?> Snapshot(BaseEntity entity)
    {
        var result = new Dictionary<string, object?>();
        var type = entity.GetType();
        foreach (var group in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).GroupBy(p => p.Name))
        {
            var prop = group.OrderByDescending(p => Depth(type, p.DeclaringType!)).First();
            if (prop.IsDefined(typeof(TrailableIgnoreAttribute)) || prop.IsDefined(typeof(JsonIgnoreAttribute))) continue;
            if (!IsScalar(prop.PropertyType)) continue;
            var value = prop.GetValue(entity);
            result[prop.Name] = value is Enum e ? e.ToString() : value;
        }
        return result;
    }

    private static int Depth(Type runtime, Type declaring)
    {
        var depth = 0;
        for (var t = runtime; t is not null && t != declaring; t = t.BaseType) depth++;
        return -depth;
    }

    private static bool IsScalar(Type t)
    {
        t = Nullable.GetUnderlyingType(t) ?? t;
        return t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(Guid) || t == typeof(decimal)
            || t == typeof(DateTime) || t == typeof(DateOnly) || t == typeof(TimeOnly) || t == typeof(DateTimeOffset);
    }
}
