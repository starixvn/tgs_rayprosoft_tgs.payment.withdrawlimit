using FluentdForward.OpenTelemetry.MessagePack;
using Microsoft.Extensions.Options;
using NATS.Client.Core;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.MessageQueue.Nats.Serializers;
using Tgs.Payment.WithdrawLimit.MessageQueue;
using Tgs.Payment.WithdrawLimit.Models;

var builder = WebApplication.CreateBuilder(args);
{
	builder.Configuration.AddJsonFile("appsettings.json",
		optional: true,
		reloadOnChange: true
);
builder.WebHost.UseUrls($"http://0.0.0.0:{builder.Configuration["HostingPort"]}");
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
			builder.Configuration.GetValue<Uri>("WithdrawLimit:BrandCertificateBaseUrl")!)
		.AddPaymentServiceClient(httpBuilder => httpBuilder
			.ConfigureHttpClient(http => http.BaseAddress =
				builder.Configuration.GetValue<Uri>("WithdrawLimit:PaymentServiceBaseUrl")!));

	builder.Services
		.AddKeyedSingleton(
			"Nats",
			(sp, _) => new NatsConnection(NatsOpts.Default with
			{
				Url = builder.Configuration.GetConnectionString("Nats")!,
				SerializerRegistry = TgsNatsSerializerRegistration.Default
			}))
		.AddMessageQueue()
		.AddNatsMessageQueue(natsConfig => natsConfig
			.ConfigureResolveConnection(sp => sp.GetRequiredKeyedService<NatsConnection>("Nats"))
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

	var excludeExtensions = new[] { "html", "ico", "png", "jpg" };
	builder.Services
		.AddHealthChecks()
		.Services		
		.AddOpenTelemetry()
		.WithTracing(tracingBuilder => tracingBuilder
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddSource("Tgs.*")
			.AddSource("TGS.*")
			.AddSource("Valhalla.*")
			.AddAspNetCoreInstrumentation(options => options.Filter =
				httpContext => !httpContext.Request.Path.HasValue
					|| !httpContext.Request.Path.StartsWithSegments("/swagger")
					&& !httpContext.Request.Path.StartsWithSegments("/healthz")
					&& !httpContext.Request.Path.StartsWithSegments("/metrics")
					&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
					&& !excludeExtensions.Any(
						ext => httpContext.Request.Path.Value.EndsWith($".{ext}")))
			.AddProfileViewExporter()
			.AddOtlpExporter())
		.WithMetrics(meterBuiler => meterBuiler
			.SetResourceBuilder(ResourceBuilder.CreateDefault()
				.AddEnvironmentVariableDetector())
			.AddRuntimeInstrumentation()
			.AddAspNetCoreInstrumentation()
			.AddOtlpExporter())
		.Services
		.AddProfileViewer();
}

var app = builder.Build();
{
	app.MapHealthChecks("/healthz");
	app.MapProfileViewer();
}

app.Run();
