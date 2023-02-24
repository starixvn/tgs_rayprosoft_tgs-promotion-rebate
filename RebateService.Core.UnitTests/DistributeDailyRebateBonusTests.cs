using System;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.BrandCertificate.Clients;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.SerialNumberGenerator;
using Tgs.Services.Rebate;
using Tgs.Services.Rebate.ApplicationServices;

namespace RebateService.Core.UnitTests;

public class DistributeDailyRebateBonusTests
{
	[Fact]
	public async Task 輸入指定日期_發放每日反水()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new DistributeDailyRebateBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeDailyRebateBonusCommandService>.Instance);

		var date = DateTimeOffset.Parse("2022-12-8 00:00:00+08");

		var command = new DistributeDailyRebateBonusCommand(date.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate = command.ScheduleDateTime.AddDays(-1);

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(
				"""
		{
			"16": 480,
			"17": 420
		}
		"""
			);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>()).Returns(new[]
		{
			brandId
		}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.ListPromotionsAsync(Arg.Any<Guid>()).Returns(new[]
		{
			new PromotionView
			{
				Id = promotionId,
				UsedAmount = 0M
			}
		}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebate"))
			.Returns(new[]
			{
				new PromotionMainInfo()
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new During(DateTimeOffset.Parse("2022-12-5 16:00:00+00").UtcDateTime,
						DateTimeOffset.Parse("2022-12-10 16:00:00+00").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetRebateUnDistributedAsync(Arg.Is(promotionId), Arg.Is(startDate), Arg.Is(startDate))
			.Returns(Array.Empty<PromotionRebateDistribute>());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionRebateSettlementsAsync(
				Arg.Any<Guid>(),
				 Arg.Is(DateTimeOffset.Parse("2022-12-6 16:00:00+00").UtcDateTime),
				 Arg.Is(DateTimeOffset.Parse("2022-12-7 16:00:00+00").UtcDateTime));
	}

	[Fact]
	public async Task 輸入指定日期_發放每日反水_()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();
		var fakeBrandServiceClient = Substitute.For<IBrandServiceClient>();
		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();

		var sut = new DistributeDailyRebateBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider,
			fakeBrandServiceClient,
			NullLogger<DistributeDailyRebateBonusCommandService>.Instance);

		var date = DateTimeOffset.Parse("2022-12-8 8:00:00+00");

		var command = new DistributeDailyRebateBonusCommand(date.UtcDateTime);

		var promotionId = Guid.NewGuid();
		var brandId = Guid.NewGuid();
		var startDate = command.ScheduleDateTime.AddDays(-1);

		_ = fakeTgsRepository.GetTimeSpanMinutesAsync(Arg.Any<string>())
			.Returns(
				"""
		{
			"16": 480,
			"17": 420,
			"8":-480
		}
		"""
			);

		_ = fakeBrandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(Arg.Any<int>()).Returns(new[]
		{
			brandId
		}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.ListPromotionsAsync(Arg.Any<Guid>()).Returns(new[]
		{
			new PromotionView
			{
				Id = promotionId,
				UsedAmount = 0M
			}
		}.ToAsyncEnumerable());

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebate"))
			.Returns(new[]
			{
				new PromotionMainInfo()
				{
					Id = promotionId,
					BrandId = brandId,
					EventDuring = new During(DateTimeOffset.Parse("2022-12-5 16:00:00+00").UtcDateTime,
						DateTimeOffset.Parse("2022-12-10 16:00:00+00").UtcDateTime)
				}
			}.ToAsyncEnumerable());

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IPromotionServiceClient)))
			.Returns(fakePromotionServiceClient);

		_ = fakeTgsRepository
			.GetRebateUnDistributedAsync(Arg.Is(promotionId), Arg.Is(startDate), Arg.Is(startDate))
			.Returns(Array.Empty<PromotionRebateDistribute>());

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionRebateSettlementsAsync(
				Arg.Any<Guid>(),
				 Arg.Is(DateTimeOffset.Parse("2022-12-7 8:00:00+00").UtcDateTime),
				 Arg.Is(DateTimeOffset.Parse("2022-12-8 8:00:00+00").UtcDateTime));
	}

}
