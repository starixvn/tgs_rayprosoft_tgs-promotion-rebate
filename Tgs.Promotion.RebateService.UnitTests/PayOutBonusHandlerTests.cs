using Google.Protobuf;
using NSubstitute;
using Tgs.Promotion.RebateService.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.UnitTests;

public class PayOutBonusHandlerTests
{
	[Fact]
	public async Task 執行將反水提撥到玩家錢包()
	{
		// Arrange
		var fakePayOutService = Substitute.For<ICommandService<PayOutCommand>>();

		var sut = new PayOutBonusHandler(fakePayOutService);

		var request = new BonusTransferRequest
		{
			DistributeId = Guid.NewGuid()
		};

		// Act
		await sut.HandleAsync(request.ToByteArray());

		// Assert
		_ = fakePayOutService.Received(1)
			.ExecuteAsync(Arg.Is(new PayOutCommand(request.DistributeId)));
	}
}
