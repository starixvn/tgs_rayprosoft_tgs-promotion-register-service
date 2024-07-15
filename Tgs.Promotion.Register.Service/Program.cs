using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tgs.Promotion.Register.Service.MessageReply;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddMessageQueue()
	.AddNatsMessageQueue(natsConfig => natsConfig
		.ConfigQueueOptions((options, sp) =>
		{
			var config = sp.GetRequiredService<IConfiguration>();

			options.Url = config.GetConnectionString("Nats")!;
		})
		.AddProcessor<RegisterRuleReplyProcessor>("tgs.promotion.register.setting", "tgs.promotion.register.setting")
		.AddProcessor<RegisterMessageProcessor>("tgs.promotion.register.distribute", "tgs.promotion.register.distribute"))
	.AddNatsGlobPatternExchange("*");

builder.Logging
	.AddOpenTelemetry(options =>
	{
		options.IncludeFormattedMessage = true;
		options.ParseStateValues = true;
		_ = options.AddFluentdForwardExporter(fluentOptions =>
		{
			var nodeIp = builder.Configuration.GetValue<string>("NODE_IP")!;
			fluentOptions.Host = nodeIp;
			fluentOptions.Tag = builder.Configuration.GetValue<string>("OTEL_SERVICE_NAME")!;
			fluentOptions.UseMessagePack();
		});
	});

builder.Services
	.AddOptions<OtlpExporterOptions>()
	.Configure<IConfiguration>((options, conifg) =>
	{
		var nodeIp = conifg.GetValue<string>("NODE_IP");

		if (!string.IsNullOrWhiteSpace(nodeIp))
			options.Endpoint = new Uri($"http://{nodeIp}:4317");

		conifg.GetSection("Opentelemetry:Tracing").Bind(options);
	});

builder.Services
	.AddOpenTelemetry()
	.WithTracing(builder => builder
		.SetResourceBuilder(ResourceBuilder.CreateDefault()
			.AddEnvironmentVariableDetector())
		.AddSource("Tgs.*")
		.AddSource("TGS.*")
		.AddAspNetCoreInstrumentation()
		.AddHttpClientInstrumentation()
		.AddEntityFrameworkCoreInstrumentation(options => options.SetDbStatementForText = true)
		.AddProfileViewExporter()
		.AddOtlpExporter())
	.Services
	.Configure<AspNetCoreTraceInstrumentationOptions>(options => options.Filter =
		httpContext => !httpContext.Request.Path.HasValue
			|| !httpContext.Request.Path.StartsWithSegments("/swagger")
			&& !httpContext.Request.Path.StartsWithSegments("/health")
			&& !httpContext.Request.Path.StartsWithSegments("/profiler/")
			&& !new[] { "html", "ico", "png", "jpg" }.Any(
				ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));
builder.Services.AddProfileViewer();

builder.Services.AddPromotionClientServices((options, sp) =>
{
	var config = sp.GetRequiredService<IConfiguration>();

	options.Url = config.GetValue<Uri>("Promotion:Url")!;
});

builder.Services
	.AddTgsRepository(builder.Configuration.GetConnectionString("Tgs")!)
	.AddRedis((redisOptions, _)
		=> redisOptions.ConnectionString = builder.Configuration.GetConnectionString("Redis")!)
	.AddPromotionRegisterCore()
	.AddPlayerServiceClient((options, sp) =>
	{
		var config = sp.GetRequiredService<IConfiguration>();
		options.BaseUrl = config.GetValue<Uri>("Promotion:PlayerServiceBaseUrl")!;
	})
	.AddMemoryCache();

builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwaggerUI();
app.MapControllers();
app.MapSwagger();
app.MapHealthChecks("/healthz");

app.Run();
