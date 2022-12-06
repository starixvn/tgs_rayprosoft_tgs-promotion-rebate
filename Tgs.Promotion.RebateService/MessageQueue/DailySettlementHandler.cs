using Tgs.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.MessageQueue;

public class DailySettlementHandler : IMessageHandler
{
    private readonly ICommandService<CalculateDailyRebateCommand> _calculateService;

    public DailySettlementHandler(ICommandService<CalculateDailyRebateCommand> calculateService)
    {
        _calculateService = calculateService ?? throw new ArgumentNullException(nameof(calculateService));
    }

    public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var request = DailySettlementRequest.Parser.ParseFrom(data.Span);

        var command = new CalculateDailyRebateCommand(request.ReportDate);

        return _calculateService.ExecuteAsync(command, cancellationToken);
    }
}
