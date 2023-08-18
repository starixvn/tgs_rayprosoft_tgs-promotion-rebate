using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;
using Tgs.Services.Rebate;
using Tgs.Services.Rebate.ApplicationServices;

namespace RebateService.Core.UnitTests;

public class CalculateDailyRebateTests
{
	[Fact]
	public async Task 輸入指定日期_統計每日反水()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new CalculateDailyRebateCommandService(
			fakeTgsRepository,
			fakePromotionServiceClient,
			fakeBrandServiceClient,
			NullLogger<CalculateDailyRebateCommandService>.Instance);

		var date = DateTimeOffset.Parse("2022-11-29 16:00:00+00");
		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420
			}
			""";

		var command = new CalculateDailyRebateCommand(date.DateTime);

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(timeSpanMinutes);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>())
			.Returns(Array.Empty<Guid>().ToAsyncEnumerable());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		var reportDateTime = new DateTimeOffset(command.SettleDateTime).ToOffset(TimeSpan.FromMinutes(480)).AddDays(-1);

		var reportDate = new DateTimeOffset(reportDateTime.Year, reportDateTime.Month, reportDateTime.Day, 0, 0,
			0, reportDateTime.Offset);

		_ = fakeTgsRepository.Received(1)
			.QueryGameRoundStatisticsAsync(Arg.Is(reportDate), Arg.Any<Guid[]>());

		_ = fakePromotionServiceClient.Received(1)
			.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebate"));
		
		_ = fakeTgsRepository.Received(1)
			.SavePromotionRebateSettlementsAsync(reportDate, Arg.Any<IEnumerable<PromotionRebateSettlement>>());
	}

	[Fact]
	public async Task Settle_Rebate_Daily_From_Game_Round_Aggregate_For_India()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new CalculateDailyRebateCommandService(
			fakeTgsRepository,
			fakePromotionServiceClient,
			fakeBrandServiceClient,
			NullLogger<CalculateDailyRebateCommandService>.Instance);

		var brandId = Guid.NewGuid();
		var date = DateTimeOffset.Parse("2022-11-29 19:00:00+00");
		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420,
			    "19": 330
			}
			""";

		var command = new CalculateDailyRebateCommand(date.DateTime);

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(timeSpanMinutes);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>())
			.Returns(new[] { brandId }.ToAsyncEnumerable());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		var reportDateTime = new DateTimeOffset(command.SettleDateTime).ToOffset(TimeSpan.FromMinutes(330)).AddDays(-1);

		var reportDate = new DateTimeOffset(reportDateTime.Year, reportDateTime.Month, reportDateTime.Day, 0, 0,
			0, reportDateTime.Offset);

		_ = fakeTgsRepository.Received(1)
			.QueryGameRoundStatisticsFromGameRoundAggregateAsync(Arg.Is(reportDate), Arg.Is(brandId));
		_ = fakeTgsRepository.Received(0)
			.QueryGameRoundStatisticsAsync(Arg.Is(reportDate), Arg.Any<Guid[]>());

		_ = fakePromotionServiceClient.Received(1)
			.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebate"));

		_ = fakeTgsRepository.Received(1)
			.SavePromotionRebateSettlementsAsync(reportDate, Arg.Any<IEnumerable<PromotionRebateSettlement>>());
	}

	[Fact]
	public async Task Settle_Rebate_Daily_For_India_Time_Zone()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();

		var sut = new CalculateDailyRebateCommandService(
			fakeTgsRepository,
			fakePromotionServiceClient,
			fakeBrandServiceClient,
			NullLogger<CalculateDailyRebateCommandService>.Instance);

		var brandId = Guid.NewGuid();
		var date = DateTimeOffset.Parse("2022-11-29 19:00:00+00");
		var timeSpanMinutes = """
			{
				"16": 480,
				"17": 420,
			    "19": 330
			}
			""";

		var command = new CalculateDailyRebateCommand(date.DateTime);

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(timeSpanMinutes);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Is(330))
			.Returns(new[] { brandId }.ToAsyncEnumerable());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		var reportDateTime = new DateTimeOffset(command.SettleDateTime).ToOffset(TimeSpan.FromMinutes(330)).AddDays(-1);

		var reportDate = new DateTimeOffset(reportDateTime.Year, reportDateTime.Month, reportDateTime.Day, 0, 0,
			0, reportDateTime.Offset);

		_ = fakeBrandServiceClient.Received(1).GetBrandIdsByTimeSpanMinutesAsync(Arg.Is(330));

		_ = fakeTgsRepository.Received(1)
			.QueryGameRoundStatisticsFromGameRoundAggregateAsync(Arg.Is(reportDate), Arg.Is(brandId));

		_ = fakeTgsRepository.Received(0)
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
