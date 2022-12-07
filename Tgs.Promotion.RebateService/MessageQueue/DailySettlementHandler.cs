using Tgs.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.MessageQueue;

public class DailySettlementHandler : IMessageHandler
{
	private readonly ICommandService<CalculateDailyRebateCommand> _calculateService;
	private readonly ILogger<DailySettlementHandler> _logger;

	public DailySettlementHandler(
		ICommandService<CalculateDailyRebateCommand> calculateService,
		ILogger<DailySettlementHandler> logger)
	{
		_calculateService = calculateService ?? throw new ArgumentNullException(nameof(calculateService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = DailySettlementRequest.Parser.ParseFrom(data.Span);

		var command = new CalculateDailyRebateCommand(request.ReportDate);

		_logger.LogInformation("Process daily settle {date}", request.ReportDate.ToDateOnly());

		return _calculateService.ExecuteAsync(command, cancellationToken);
	}
}
