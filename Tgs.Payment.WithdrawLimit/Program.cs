using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Payment.WithdrawLimit.MessageQueue;
using Tgs.Payment.WithdrawLimit.Models;

var builder = WebApplication.CreateBuilder(args);
{
	builder.Logging
		.AddOpenTelemetry(options =>
		{
			options.IncludeFormattedMessage = true;
			options.ParseStateValues = true;
			options.AddFluentdForwardExporter(fluentOptions =>
			{
				var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
				fluentOptions.Host = nodeIp;
				fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
				fluentOptions.UseMessagePack(LogRecordFormatterResolver.GetResolverInstanceWithExtendInfo(
					"MachineName",
					() => Environment.MachineName));
			});
		});

	builder.Services
		.AddSingleton(sp => ActivatorUtilities.CreateInstance<RedisConnectionManager>(
			sp,
			builder.Configuration.GetConnectionString("Redis")!))
		.AddWithdrawLimit()
		.AddWithdrawLimitRepositories(sp => sp.GetRequiredService<RedisConnectionManager>().Database)
		.AddBrandServiceClient((options, _) => options.BrandCertificateBaseUrl =
			builder.Configuration.GetValue<Uri>("WithdrawLimit:BrandCertificateBaseUrl")!);

	builder.Services
		.AddPhoenixToolkitsMessageQueue()
		.AddNatsMessageQueue(natsConfig => natsConfig
			.ConfigQueueOptions((options, _) => options.Url = builder.Configuration.GetConnectionString("Nats")!)
			.AddProcessor(
				ProcessorTypes.IncreaseWithdrawProcessorType,
				"tgs.payment.withdraw-limit.increase",
				"tgs.payment.withdraw-limit.increase")
			.AddProcessor(
				ProcessorTypes.DecreaseWithdrawProcessorType,
				"tgs.payment.withdraw-limit.decrease",
				"tgs.payment.withdraw-limit.decrease")
			.AddHandler(
				ProcessorTypes.WithdrawFlowHandlerType,
				"tgs.payment.withdraw.status_ex",
				"tgs.withdraw-limit.withdraw-flow"))
		.AddNatsGlobPatternExchange("*");

	builder.Services
		.AddHealthChecks()
		.Services
		.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
			sp => new ConfigureNamedOptions<OtlpExporterOptions>(
				Options.DefaultName,
				exporterOptions =>
				{
					var config = sp.GetRequiredService<IConfiguration>();
					var nodeIp = config.GetValue<string>("NODE_IP");

					if (!string.IsNullOrWhiteSpace(nodeIp))
						exporterOptions.Endpoint = new Uri($"http://{nodeIp}:4317");

					config.GetSection("Opentelemetry:Tracing").Bind(exporterOptions);
				}))
		.AddOpenTelemetry()
		.WithTracing(tracingBuilder => tracingBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddSource("Tgs.*")
			.AddSource("TGS.*")
			.AddProfileViewExporter()
			.AddOtlpExporter())
		.WithMetrics(meterBuiler => meterBuiler
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddRuntimeInstrumentation()
			.AddAspNetCoreInstrumentation()
			.AddOtlpExporter())
		.Services
		.Configure<AspNetCoreInstrumentationOptions>(
			options => options.Filter = httpContext => !httpContext.Request.Path.HasValue
				|| !httpContext.Request.Path.StartsWithSegments("/swagger")
				&& !httpContext.Request.Path.StartsWithSegments("/healthz")
				&& !httpContext.Request.Path.StartsWithSegments("/metrics")
				&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
				&& !new[] { "html", "ico", "png", "jpg" }.Any(
					ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
		.AddProfileViewer();
}

var app = builder.Build();
{
	app.MapHealthChecks("/healthz");
	app.MapProfileViewer();
}

app.Run();
