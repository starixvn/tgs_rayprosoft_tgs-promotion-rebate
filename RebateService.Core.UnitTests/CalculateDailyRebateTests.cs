using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

using Tgs.Promotion.Clients;
using Tgs.Services.Rebate;
using Tgs.Services.Rebate.ApplicationServices;
using Xunit;

namespace RebateService.Core.UnitTests;

public class CalculateDailyRebateTests
{
	[Fact]
	public async Task 輸入指定日期_統計每日反水()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new CalculateDailyRebateCommandService(
			fakeTgsRepository,
			fakePromotionServiceClient,
			NullLogger<CalculateDailyRebateCommandService>.Instance);

		var command = new CalculateDailyRebateCommand(DateOnly.Parse("2022-11-29"));

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryGameRoundStatisticsAsync(Arg.Is(command.ReportDate));
		_ = fakePromotionServiceClient.Received(1)
			.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebate"));
		_ = fakeTgsRepository.Received(1)
			.SavePromotionRebateSettlementsAsync(Arg.Any<IEnumerable<PromotionRebateSettlement>>());
	}

	[Fact]
	public void 日期區間比對_Between()
	{
		// Arrange
		var sut = new During(
			DateTime.Parse("2022-12-06 16:00:00.000000 +00:00"),
			DateTime.Parse("2022-12-07 16:00:00.000000 +00:00"));

		var queryDate = DateOnly.Parse("2022-12-7");

		// Act
		var actual = queryDate.Between(sut.From.ToDateOnly(), sut.To.ToDateOnly());

		// Assert
		Assert.True(actual);
	}
}
