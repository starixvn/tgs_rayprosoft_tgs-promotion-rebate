using Google.Protobuf;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Promotion.RebateService.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.UnitTests;

public class DailyDistributeBonusHandlerTests
{
	[Fact]
	public async Task 處理排程每日反水發放()
	{
		// Arrange
		var fakeDistributeService = Substitute.For<ICommandService<DistributeDailyRebateBonusCommand>>();
		var fakeWeeklyDistributeService = Substitute.For<ICommandService<DistributeWeeklyRebateBonusCommand>>();

		var sut = new DailyDistributeBonusHandler(
			fakeDistributeService,
			fakeWeeklyDistributeService,
			NullLogger<DailyDistributeBonusHandler>.Instance);

		var request = new RebateDistributeRequest
		{
			DistributeDate = DateOnly.Parse("2022-12-8")
		};

		// Act
		await sut.HandleAsync(request.ToByteArray());

		// Assert
		_ = fakeDistributeService.Received(1)
			.ExecuteAsync(Arg.Is(new DistributeDailyRebateBonusCommand(request.DistributeDate)));
		_ = fakeWeeklyDistributeService.Received(1)
			.ExecuteAsync(Arg.Is(new DistributeWeeklyRebateBonusCommand(request.DistributeDate)));
	}
}
