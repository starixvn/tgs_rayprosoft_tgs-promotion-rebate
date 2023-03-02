using Microsoft.Extensions.Options;
using Tgs.Promotion.RebateService.Client;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromotionRebateClient(
		this IServiceCollection services,
		Action<PromotionOptions, IServiceProvider> configure)
		=> services
			.AddOptions<PromotionOptions>()
			.Configure(configure)
			.Services
			.AddHttpClient<PromotionRebateClient>((sp, c) =>
			{
				var options = sp.GetRequiredService<IOptions<PromotionOptions>>().Value;

				c.BaseAddress = options.Url;
			})
			.Services
			.AddTransient<IPromotionRebateClient, PromotionRebateClient>();
}
