using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using OpenTelemetry.Exporter;
using RebateService.Repositories.Entities;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;

namespace Tgs.Promotion.RebateService.IntegrationTest;

extern alias promotion;

internal class TestPromotionApplication : WebApplicationFactory<promotion.Program>
{
	private readonly Action<IWebHostBuilder>? _webHostConfigure;

	public TestPromotionApplication(Action<IWebHostBuilder>? webHostConfigure = null)
	{
		_webHostConfigure = webHostConfigure;
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder)
	{
		_ = builder
			.ConfigureAppConfiguration(builder => builder.Sources.Clear())
			.ConfigureLogging(logging => logging.ClearProviders().AddConsole());

		_ = builder.ConfigureServices(services => services
			.AddSingleton<IConfigureOptions<OtlpExporterOptions>>(
				sp => new ConfigureNamedOptions<OtlpExporterOptions>(
					Options.DefaultName,
					exporterOptions =>
					{
						exporterOptions.TimeoutMilliseconds = 0;
						exporterOptions.ExportProcessorType = OpenTelemetry.ExportProcessorType.Batch;
						exporterOptions.Endpoint = new Uri("http://otel.rpgp.cc:4317");
					}))
			.AddFakeNatsMessageQueue()
			.Replace(ServiceDescriptor.Singleton(sp =>
			{
				var dbContextOptionsBuilder = new DbContextOptionsBuilder<TgsContext>();
				_ = dbContextOptionsBuilder
					.UseInMemoryDatabase(Guid.NewGuid().ToString())
					.ConfigureWarnings(x => x.Ignore(
						InMemoryEventId.TransactionIgnoredWarning));

				return new TgsContext(dbContextOptionsBuilder.Options);
			}))
			.AddSingleton(Substitute.For<IPromotionClient>())
			.AddSingleton(Substitute.For<IPromotionServiceClient>())
			.AddSingleton(Substitute.For<IBrandServiceClient>()));

		_webHostConfigure?.Invoke(builder);
	}
}
