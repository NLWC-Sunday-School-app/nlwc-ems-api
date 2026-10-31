using Microsoft.Extensions.Hosting;
using Serilog;

namespace nlwc.ems.Base.Web.Extensions;

public static class HostBuilderExtensions
{
    public static IHostBuilder UseEmsSerilog(this IHostBuilder hostBuilder) => hostBuilder.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .WriteTo.Console());
}
