using Tgs.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.MessageQueue;

public class SupplementaryDailySettlementHandler : IMessageHandler
{
	private readonly ICommandService<SupplementaryCalculateDailyRebateCommand> _calculateService;
	private readonly ILogger<SupplementaryDailySettlementHandler> _logger;

	public SupplementaryDailySettlementHandler(
		ICommandService<SupplementaryCalculateDailyRebateCommand> calculateService,
		ILogger<SupplementaryDailySettlementHandler> logger)
	{
		_calculateService = calculateService ?? throw new ArgumentNullException(nameof(calculateService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = DailySettlementRequest.Parser.ParseFrom(data.Span);

		var command = new SupplementaryCalculateDailyRebateCommand(request.SettleDateTime.ToDateTime());

		_logger.LogInformation("Process supplementary daily settle {date}", request.SettleDateTime.ToDateTime());

		return _calculateService.ExecuteAsync(command, cancellationToken);
	}
}
