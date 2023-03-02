using ExpectedObjects;
using NSubstitute;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.SerialNumberGenerator;
using Tgs.Services.Rebate;

namespace RebateService.Core.UnitTests;

public class PromotionDistributorTests
{
	[Fact]
	public async Task 活動反水發放如果Bouns超過最大發放值則以最大值為主()
	{
		// Arrange
		var promotionMainInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid()
		};
		var scheduleStartDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var scheduleEndDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var firstDate = DateOnly.Parse("2022-12-7");

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PromotionDistributor(
			promotionMainInfo,
			scheduleStartDate,
			scheduleEndDate,
			0M,
			fakeSerialNumberGenerator,
			fakeMessageSender,
			fakePromotionServiceClient);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),123));

		_ = fakeTgsRepository.QueryPromotionRebateSettlementsAsync(
			Arg.Is(promotionMainInfo.Id),
			Arg.Is(scheduleStartDate.UtcDateTime),
			Arg.Is(scheduleEndDate.AddDays(1).UtcDateTime))
			.Returns(new[] {
				new PromotionRebateSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					10000M,
					1000M,
					10000M,
					new RebateSettings(
						null,
						null,
						100M,
						500M,
						10M,
						10M,
						true,
						Array.Empty<int>()))
			}.ToAsyncEnumerable());

		var expectedDistributes = new[]
		{
			new
			{
				DistributeDate = firstDate,
				PlayerCode = "test1",
				Bonus = 500M,
				BucketAmount = 5000M
			}
		}.ToExpectedObject();

		// Act
		await sut.ProcessDistributesAsync(fakeTgsRepository);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.SavePromotionRebateDistributesAsync(
				Arg.Is<IEnumerable<PromotionRebateDistribute>>(
					dists => expectedDistributes.Matches(dists)));
	}

	[Fact]
	public async Task 活動反水發放會依照每日統計執行發放()
	{
		// Arrange
		var promotionMainInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid()
		};
		var scheduleStartDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var scheduleEndDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var firstDate = DateOnly.Parse("2022-12-7");

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PromotionDistributor(
			promotionMainInfo,
			scheduleStartDate,
			scheduleEndDate,
			0M,
			fakeSerialNumberGenerator,
			fakeMessageSender,
			fakePromotionServiceClient);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123));

		_ = fakeTgsRepository.QueryPromotionRebateSettlementsAsync(
			Arg.Is(promotionMainInfo.Id),
			Arg.Is(scheduleStartDate.UtcDateTime),
			Arg.Is(scheduleEndDate.UtcDateTime.AddDays(1)))
			.Returns(new[] {
				new PromotionRebateSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					10000M,
					1000M,
					10000M,
					new RebateSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>()))
			}.ToAsyncEnumerable());

		var expectedDistributes = new[]
		{
			new
			{
				DistributeDate = firstDate,
				PlayerCode = "test1",
				Bonus = 1000M,
				BucketAmount = 10000M
			}
		}.ToExpectedObject();

		// Act
		await sut.ProcessDistributesAsync(fakeTgsRepository);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.SavePromotionRebateDistributesAsync(
				Arg.Is<IEnumerable<PromotionRebateDistribute>>(
					dists => expectedDistributes.Matches(dists)));
	}

	[Fact]
	public async Task 活動反水發放會依照日期區間統計做發放()
	{
		// Arrange
		var promotionMainInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid()
		};
		var scheduleStartDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var scheduleEndDate = DateTimeOffset.Parse("2022-12-7 00:00:00+8");
		var firstDate = DateOnly.Parse("2022-12-7");

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PromotionDistributor(
			promotionMainInfo,
			scheduleStartDate,
			scheduleEndDate,
			0M,
			fakeSerialNumberGenerator,
			fakeMessageSender,
			fakePromotionServiceClient);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123));

		_ = fakeTgsRepository.QueryPromotionRebateSettlementsAsync(
			Arg.Is(promotionMainInfo.Id),
			Arg.Is(scheduleStartDate.UtcDateTime),
			Arg.Is(scheduleEndDate.UtcDateTime.AddDays(1)))
			.Returns(new[] {
				new PromotionRebateSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					10000M,
					1000M,
					10000M,
					new RebateSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionRebateSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new RebateSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionRebateSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new RebateSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionRebateSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new RebateSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionRebateSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new RebateSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>())),
				new PromotionRebateSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					1000M,
					100M,
					1000M,
					new RebateSettings(
						null,
						null,
						100M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>()))
			}.ToAsyncEnumerable());

		var expectedDistributes = new[]
		{
			new
			{
				DistributeDate = firstDate,
				PlayerCode = "test1",
				Bonus = 1500M,
				BucketAmount = 15000M
			}
		}.ToExpectedObject();

		// Act
		await sut.ProcessDistributesAsync(fakeTgsRepository);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.SavePromotionRebateDistributesAsync(
				Arg.Is<IEnumerable<PromotionRebateDistribute>>(
					dists => expectedDistributes.Matches(dists)));
	}

	[Fact]
	public async Task 活動反水發放如果有效投注未達門檻不會計算進去()
	{
		// Arrange
		var promotionMainInfo = new PromotionMainInfo
		{
			Id = Guid.NewGuid(),
			BrandId = Guid.NewGuid()
		};
		var scheduleStartDate = DateTime.Parse("2022-12-7");
		var scheduleEndDate = DateTime.Parse("2022-12-7");

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		var fakeMessageSender = Substitute.For<IMessageSender>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PromotionDistributor(
			promotionMainInfo,
			scheduleStartDate,
			scheduleEndDate,
			0M,
			fakeSerialNumberGenerator,
			fakeMessageSender,
			fakePromotionServiceClient);

		var fakeTgsRepository = Substitute.For<ITgsRepository>();

		_ = fakePromotionServiceClient.GetPromotionDistributePlanAsync(Arg.Any<Guid>(), Arg.Any<long>()).Returns(new PromotionDistributePlanInfo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 123));

		_ = fakeTgsRepository.GetRebateUnDistributedAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>())
			.Returns(Array.Empty<PromotionRebateDistribute>());

		_ = fakeTgsRepository.QueryPromotionRebateSettlementsAsync(
			Arg.Is(promotionMainInfo.Id),
			Arg.Is(scheduleStartDate),
			Arg.Is(scheduleEndDate.AddDays(1)))
			.Returns(new[] {
				new PromotionRebateSettlement(
					"test1",
					Guid.NewGuid(),
					promotionMainInfo.Id,
					10000M,
					1000M,
					10000M,
					new RebateSettings(
						null,
						null,
						20000M,
						50000M,
						10M,
						10M,
						true,
						Array.Empty<int>()))
			}.ToAsyncEnumerable());

		// Act
		await sut.ProcessDistributesAsync(fakeTgsRepository);

		// Assert
		_ = fakeTgsRepository.Received(0)
			.SavePromotionRebateDistributesAsync(
				Arg.Any<IEnumerable<PromotionRebateDistribute>>());

		_ = fakeTgsRepository.Received(0)
			.GetRebateUnDistributedAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>());
	}
}
