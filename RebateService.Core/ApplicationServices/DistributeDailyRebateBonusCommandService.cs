using Microsoft.Extensions.DependencyInjection;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Services.Rebate.ApplicationServices;

internal class DistributeDailyRebateBonusCommandService : ICommandService<DistributeDailyRebateBonusCommand>
{
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly ITgsRepository _tgsRepository;
	private readonly IServiceProvider _serviceProvider;

	public DistributeDailyRebateBonusCommandService(
		IPromotionServiceClient promotionServiceClient,
		ITgsRepository tgsRepository,
		IServiceProvider serviceProvider)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public async ValueTask ExecuteAsync(DistributeDailyRebateBonusCommand command, CancellationToken cancellationToken = default)
	{
		var promotions = await _promotionServiceClient
			.ListPromotionsByPromotionTypeAsync("TurnoverRebate", cancellationToken)
			.Where(info => command.ScheduleDate.Between(
				info.EventDuring.From.ToDateOnly(),
				info.EventDuring.To.ToDateOnly()))
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false)!;

		var distributors = promotions.ToAsyncEnumerable()
			.Select(info => ActivatorUtilities.CreateInstance<PromotionDistributor>(
				_serviceProvider,
				info,
				command.ScheduleDate));

		await foreach (var distributor in distributors.ConfigureAwait(false))
			await distributor.ProcessDistributesAsync(
				_tgsRepository,
				cancellationToken).ConfigureAwait(false);
	}
}
