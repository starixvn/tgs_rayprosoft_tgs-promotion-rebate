using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
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
		var fakePromotionClient = Substitute.For<IPromotionClient>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new CalculateDailyRebateCommandService(
			fakeTgsRepository,
			fakePromotionClient,
			fakePromotionServiceClient,
			fakeBrandServiceClient,
			NullLogger<CalculateDailyRebateCommandService>.Instance);

		var date = DateTimeOffset.Parse("2022-11-29 16:00:00+00");

		var command = new CalculateDailyRebateCommand(date.DateTime);

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(
				"""
		{
			"16": 480,
			"17": 420
		}
		"""
			);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>()).Returns(Array.Empty<Guid>().ToAsyncEnumerable());
		// Act
		await sut.ExecuteAsync(command);

		var reportDate = new DateTimeOffset(command.SettleDateTime).ToOffset(TimeSpan.FromMinutes(480)).AddDays(-1);
		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryGameRoundStatisticsAsync(Arg.Is(reportDate), Arg.Any<Guid[]>());
		_ = fakePromotionServiceClient.Received(1)
			.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebate"));
		_ = fakeTgsRepository.Received(1)
			.SavePromotionRebateSettlementsAsync(reportDate, Arg.Any<IEnumerable<PromotionRebateSettlement>>());
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
