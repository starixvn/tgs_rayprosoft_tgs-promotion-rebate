using Microsoft.Extensions.DependencyInjection;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Services.Rebate.ApplicationServices;

internal class DistributeWeeklyRebateBonusCommandService : ICommandService<DistributeWeeklyRebateBonusCommand>
{
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly ITgsRepository _tgsRepository;
	private readonly IServiceProvider _serviceProvider;

	public DistributeWeeklyRebateBonusCommandService(
		IPromotionServiceClient promotionServiceClient,
		ITgsRepository tgsRepository,
		IServiceProvider serviceProvider)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
	}

	public async ValueTask ExecuteAsync(DistributeWeeklyRebateBonusCommand command, CancellationToken cancellationToken = default)
	{
		var scheduleTime = command.ScheduleDate.ToDateTime(TimeOnly.MinValue);

		var promotions = await _promotionServiceClient
			.ListPromotionsByPromotionTypeAsync("TurnoverRebateWeekly", cancellationToken)
			.Select(info =>
			{
				var startTime = new DateTimeOffset(info.EventDuring.From)
					.ToOffset(TimeSpan.FromHours(8D));
				var endTime = new DateTimeOffset(info.EventDuring.To)
					.ToOffset(TimeSpan.FromHours(8D));

				var cycleMgr = new CycleManager("0 0 * * Mon", startTime.DateTime, endTime.DateTime);

				return (
					Info: info,
					Mgr: cycleMgr,
					PreviousTriggerDate: DateOnly.FromDateTime(cycleMgr.GetPreviousOccurrence(scheduleTime)?.AddDays(-1D) ?? startTime.DateTime),
					NextTriggerDate: DateOnly.FromDateTime(cycleMgr.GetNextOccurrence(scheduleTime) ?? endTime.DateTime));
			})
			.Where(tup => tup.NextTriggerDate == command.ScheduleDate)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false)!;

		var distributors = promotions.ToAsyncEnumerable()
			.Select(tup => ActivatorUtilities.CreateInstance<PromotionDistributor>(
				_serviceProvider,
				tup.Info,
				tup.PreviousTriggerDate,
				tup.NextTriggerDate));

		await foreach (var distributor in distributors.ConfigureAwait(false))
			await distributor.ProcessDistributesAsync(
				_tgsRepository,
				cancellationToken).ConfigureAwait(false);
	}
}
