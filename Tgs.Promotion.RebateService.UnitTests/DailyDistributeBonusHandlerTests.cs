using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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

		var date = DateTimeOffset.Parse("2022-12-8 16:00:00+00");

		var request = new RebateDistributeRequest
		{
			DistributeDateTime = Timestamp.FromDateTime(date.UtcDateTime)
		};

		// Act
		await sut.HandleAsync(request.ToByteArray());

		// Assert
		_ = fakeDistributeService.Received(1)
			.ExecuteAsync(Arg.Is(new DistributeDailyRebateBonusCommand(request.DistributeDateTime.ToDateTime())));
		_ = fakeWeeklyDistributeService.Received(1)
			.ExecuteAsync(Arg.Is(new DistributeWeeklyRebateBonusCommand(request.DistributeDateTime.ToDateTime())));
	}
}
