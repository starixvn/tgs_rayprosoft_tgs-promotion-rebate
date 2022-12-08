using Tgs.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.MessageQueue;

public class PayOutBonusHandler : IMessageHandler
{
	private readonly ICommandService<PayOutCommand> _payOutService;

	public PayOutBonusHandler(ICommandService<PayOutCommand> payOutService)
	{
		_payOutService = payOutService ?? throw new ArgumentNullException(nameof(payOutService));
	}

	public ValueTask HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = BonusTransferRequest.Parser.ParseFrom(data.Span);

		var command = new PayOutCommand(request.DistributeId);

		return _payOutService.ExecuteAsync(command, cancellationToken);
	}
}
