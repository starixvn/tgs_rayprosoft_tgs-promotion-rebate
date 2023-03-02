using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RebateService.Repositories.Entities;

using Tgs.Services.Rebate;

namespace RebateService.Repositories.DependencyInjection;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddPromotionRebateRepositories(
		this IServiceCollection services,
		Action<IServiceProvider, DbContextOptionsBuilder> dbContextConfigure)
		=> services
			.AddDbContext<TgsContext>(dbContextConfigure)
			.AddTransient<ITgsRepository, TgsRepository>();
}
