using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using RebateService.Repositories.DependencyInjection;

using Tgs.Promotion.RebateService.MessageQueue;
using Tgs.Services.Rebate.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("/config/appsettings.json", true);

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
    .AddDateOnlyTimeOnlyStringConverters()
    .AddRouting(routeOptions => routeOptions.LowercaseUrls = true)
    .AddControllers()
    .AddJsonOptions(jsonOptions =>
    {
        jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .AddMvcOptions(mvcOptions =>
    {
        mvcOptions.AddWebApiToolkitFilters();
    });

builder.Services
    .AddPromotionRebate()
    .AddPromotionRebateRepositories((_, dbOptionsBuilder) => dbOptionsBuilder
        .UseNpgsql("name=Tgs"));

builder.Services
    .AddPromotionClientServices((promotionOptions, _) =>
    {
        promotionOptions.Url = builder.Configuration.GetValue<Uri>("Rebate:PromotionBaseUrl")!;
    })
    .AddMessageQueue()
    .AddNatsMessageQueue(natsConfiguration => natsConfiguration
        .ConfigQueueOptions((options, sp) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();

            options.Url = config.GetConnectionString("Nats")!;
        })
        .AddProcessor<RebateSettingProcessor>("tgs.promotion.turnoverrebate.setting", "tgs.promotion.turnoverrebate.setting")
        .AddProcessor<RebateSettingProcessor>("tgs.promotion.turnoverrebateweekly.setting", "tgs.promotion.turnoverrebateweekly.setting"))
    .AddInProcessMessageQueue(ipcConfiguration => ipcConfiguration
        .AddHandler<DailySettlementHandler>("tgs.rebate.ipc.daily-settle"))
    .AddInProcessGlobPatternExchange("tgs.rebate.ipc.*")
    .AddNatsGlobPatternExchange("*");

builder.Services
    .AddProfileViewer()
    .AddHealthChecks()
    .Services
    .AddOpenTelemetryTracing(builder => builder
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
        .AddSource("Tgs.*")
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddProfileViewExporter()
        .AddOtlpExporter())
    .AddOpenTelemetryMetrics(meterBuiler => meterBuiler
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddEnvironmentVariableDetector())
            .AddRuntimeInstrumentation()
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter())
    .Configure<AspNetCoreInstrumentationOptions>(options => options.Filter =
        httpContext => !httpContext.Request.Path.HasValue
            || !httpContext.Request.Path.StartsWithSegments("/swagger")
            && !httpContext.Request.Path.StartsWithSegments("/healthz")
            && !httpContext.Request.Path.StartsWithSegments("/metrics")
            && !new[] { "html", "ico", "png", "jpg" }.Any(
                ext => httpContext.Request.Path.Value.EndsWith($".{ext}")));

builder.Services
    .AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Player Service",
            Version = "v1"
        });

        options.ApplyWebApiToolkitSupport();
        options.ApplyDisplayNameSchemaId();
        options.UseDateOnlyTimeOnlyStringConverters();
    });

var app = builder.Build();

app.MapControllers();
app.MapSwagger();
app.UseSwaggerUI();
app.MapHealthChecks("/healthz");
app.MapProfileViewer();

app.Run();
