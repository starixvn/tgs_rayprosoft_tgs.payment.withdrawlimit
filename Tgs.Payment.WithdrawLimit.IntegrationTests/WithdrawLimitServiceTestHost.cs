using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;

namespace Tgs.Payment.WithdrawLimit.IntegrationTests;

internal static class WithdrawLimitServiceTestHost
{
    public static WebApplicationFactory<Program> Instance => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(hostBuilder => hostBuilder
            .ConfigureAppConfiguration(configBuilder => configBuilder.Sources.Clear())
            .ConfigureServices(services => services
                .AddSingleton<IConfigureOptions<OtlpExporterOptions>>(sp => new ConfigureNamedOptions<OtlpExporterOptions>(
                    Options.DefaultName,
                    exporterOptions => exporterOptions.TimeoutMilliseconds = 0))
                .AddFakeNatsMessageQueue()));
}
