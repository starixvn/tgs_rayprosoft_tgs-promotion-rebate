using Tgs.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.MessageQueue;

public class DailyDistributeBonusHandler : IMessageHandler
{
	private readonly ICommandService<DistributeDailyRebateBonusCommand> _distributeService;
	private readonly ILogger<DailyDistributeBonusHandler> _logger;

	public DailyDistributeBonusHandler(
		ICommandService<DistributeDailyRebateBonusCommand> distributeService,
		ILogger<DailyDistributeBonusHandler> logger)
	{
		_distributeService = distributeService ?? throw new ArgumentNullException(nameof(distributeService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = RebateDistributeRequest.Parser.ParseFrom(data.Span);

		var command = new DistributeDailyRebateBonusCommand(request.DistributeDate);

		_logger.LogInformation("Process daily settle {date}", request.DistributeDate.ToDateOnly());

		return _distributeService.ExecuteAsync(command, cancellationToken);
	}
}
