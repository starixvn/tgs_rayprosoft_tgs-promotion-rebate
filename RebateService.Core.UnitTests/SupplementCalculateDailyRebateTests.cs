using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;
using Tgs.Services.Rebate;
using Tgs.Services.Rebate.ApplicationServices;

namespace RebateService.Core.UnitTests;

public class SupplementCalculateDailyRebateTests
{
	[Fact]
	public async Task 輸入指定日期_補統計每週反水()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new SupplementCalculateDailyRebateCommandService(
			fakeTgsRepository,
			fakePromotionServiceClient,
			fakeBrandServiceClient,
			NullLogger<SupplementCalculateDailyRebateCommandService>.Instance);

		var brandId = Guid.NewGuid();
		var promotionId = Guid.NewGuid();
		var date = DateTimeOffset.Parse("2022-11-29 16:00:00+00");
		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420
			}
			""";

		var command = new SupplementCalculateDailyRebateCommand(date.UtcDateTime);

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(timeSpanMinutes);

		_ = fakeTgsRepository.QueryGameRoundStatisticsAsync(Arg.Any<DateTimeOffset>(), Arg.Any<Guid[]>()).Returns(
			new GameRoundStatistics[]
			{
				new GameRoundStatistics("test1", 1, brandId, Guid.NewGuid(), GameType.Sportsbook, 100M)
			}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Any<string>()).Returns(
			new PromotionMainInfo[]
			{
				new PromotionMainInfo
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new During
					{
						From = DateTime.Parse("2022-10-29 16:00:00+00"),
						To = DateTime.Parse("2022-12-29 16:00:00+00")
					}
				}
			}.ToAsyncEnumerable());

		_ = fakeTgsRepository.GetPlayerCodesByDailyStatisticsReportDateAsync(Arg.Any<DateTimeOffset>(), Arg.Any<Guid[]>())
			.Returns(new[]{ "test1" }.ToAsyncEnumerable());

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>())
			.Returns(new[]{ brandId }.ToAsyncEnumerable());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		var reportDateTime = new DateTimeOffset(command.SettleDateTime).ToOffset(TimeSpan.FromMinutes(480)).AddDays(-1);

		var reportDate = new DateTimeOffset(reportDateTime.Year, reportDateTime.Month, reportDateTime.Day, 0, 0,
			0, reportDateTime.Offset);

		_ = fakeTgsRepository.Received(1)
			.QueryGameRoundStatisticsAsync(Arg.Is(reportDate), Arg.Is<Guid[]>(x => new[] { brandId }.SequenceEqual(x)));

		_ = fakeTgsRepository.Received(1)
			.GetPlayerCodesByDailyStatisticsReportDateAsync(Arg.Is(reportDate), Arg.Is<IEnumerable<Guid>>(x => new[] { promotionId }.SequenceEqual(x)));

		_ = fakePromotionServiceClient.Received(1)
			.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebateWeekly"));

		_ = fakeTgsRepository.Received(1)
			.SavePromotionRebateSettlementsAsync(Arg.Is(reportDate), Arg.Any<IEnumerable<PromotionRebateSettlement>>());
	}
}
