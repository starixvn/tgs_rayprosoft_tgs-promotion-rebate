using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Tgs.Services.Rebate.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromotionRebate(this IServiceCollection services)
		=> services
			.RegisterAllServiceUtilityTypes(Assembly.GetExecutingAssembly(), ServiceLifetime.Transient);
}