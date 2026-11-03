using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace nlwc.ems.Data.EFCore;

/// <summary>Used only by <c>dotnet ef</c>; never connects. Keeps tooling independent of the app's startup.</summary>
public class EmsDbContextFactory : IDesignTimeDbContextFactory<EmsDbContext>
{
    public EmsDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<EmsDbContext>()
        .UseMySql("Server=localhost;Database=nlwc_ems_design", new MySqlServerVersion(new Version(8, 0, 36)))
        .UseSnakeCaseNamingConvention()
        .Options);
}
