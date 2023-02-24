using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Promotion.RebateService.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.Services.Rebate.ApplicationServices;
using Tgs.ServiceUtility;

namespace Tgs.Promotion.RebateService.UnitTests;

public class DailySettlementHandlerTests
{
	[Fact]
	public async Task 處理排程觸發結算每日反水統計()
	{
		// Arrange
		var fakeCalculateService = Substitute.For<ICommandService<CalculateDailyRebateCommand>>();

		var sut = new DailySettlementHandler(
			fakeCalculateService,
			NullLogger<DailySettlementHandler>.Instance);

		var date = DateTimeOffset.Parse("2022-12-8 16:00:00+00");

		var request = new DailySettlementRequest
		{
			SettleDateTime = Timestamp.FromDateTime(date.UtcDateTime)
		};

		// Act
		await sut.HandleAsync(request.ToByteArray());

		// Assert
		_ = fakeCalculateService.Received(1)
			.ExecuteAsync(Arg.Is(new CalculateDailyRebateCommand(request.SettleDateTime.ToDateTime())));
	}
}
