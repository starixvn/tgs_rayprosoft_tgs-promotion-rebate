using Tgs.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.MessageQueue;

public class DailyDistributeBonusHandler : IMessageHandler
{
	private readonly ICommandService<DistributeDailyRebateBonusCommand> _distributeService;
	private readonly ICommandService<DistributeWeeklyRebateBonusCommand> _weeklyDistributeService;
	private readonly ILogger<DailyDistributeBonusHandler> _logger;

	public DailyDistributeBonusHandler(
		ICommandService<DistributeDailyRebateBonusCommand> distributeService,
		ICommandService<DistributeWeeklyRebateBonusCommand> weeklyDistributeService,
		ILogger<DailyDistributeBonusHandler> logger)
	{
		_distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
		_weeklyDistributeService = weeklyDistributeService ?? throw new ArgumentNullException(nameof(weeklyDistributeService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RebateDistributeRequest.Parser.ParseFrom(data.Span);

		_logger.LogInformation("Process daily settle {date}", request.DistributeDate.ToDateOnly());

		await Task.WhenAll(
			_distributeService.ExecuteAsync(
				new DistributeDailyRebateBonusCommand(request.DistributeDate),
				cancellationToken).AsTask(),
			_weeklyDistributeService.ExecuteAsync(
				new DistributeWeeklyRebateBonusCommand(request.DistributeDate),
				cancellationToken).AsTask()).ConfigureAwait(false);
	}
}
