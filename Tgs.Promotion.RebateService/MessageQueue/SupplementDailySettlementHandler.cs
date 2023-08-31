using Tgs.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.MessageQueue;

public class SupplementDailySettlementHandler : IMessageHandler
{
	private readonly ICommandService<SupplementCalculateDailyRebateCommand> _calculateService;
	private readonly ILogger<SupplementDailySettlementHandler> _logger;

	public SupplementDailySettlementHandler(
		ICommandService<SupplementCalculateDailyRebateCommand> calculateService,
		ILogger<SupplementDailySettlementHandler> logger)
	{
		_calculateService = calculateService ?? throw new ArgumentNullException(nameof(calculateService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = DailySettlementRequest.Parser.ParseFrom(data.Span);

		var command = new SupplementCalculateDailyRebateCommand(request.SettleDateTime.ToDateTime());

		_logger.LogInformation("Process supplement daily settle {date}", request.SettleDateTime.ToDateTime());

		return _calculateService.ExecuteAsync(command, cancellationToken);
	}
}
