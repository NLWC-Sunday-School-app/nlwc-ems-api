using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace nlwc.ems.Data.EFCore.EntityConfigurations;

public static class EntityTypeBuilderExtensions
{
    /// <summary>The columns every <see cref="BaseEntity"/> carries, configured once: string-stored status and the
    /// audit stamps' lengths.</summary>
    public static EntityTypeBuilder<T> ConfigureBase<T>(this EntityTypeBuilder<T> builder, string table) where T : BaseEntity
    {
        builder.ToTable(table);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(e => e.CreatedBy).HasMaxLength(50);
        builder.Property(e => e.ModifiedBy).HasMaxLength(50);
        builder.Property(e => e.CreatedByIp).HasMaxLength(64);
        builder.Property(e => e.ModifiedByIp).HasMaxLength(64);
        return builder;
    }
}
