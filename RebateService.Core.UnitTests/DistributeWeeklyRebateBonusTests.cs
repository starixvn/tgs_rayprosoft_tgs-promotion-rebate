using NSubstitute;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.SerialNumberGenerator;
using Tgs.Services.Rebate;
using Tgs.Services.Rebate.ApplicationServices;
using Xunit;

namespace RebateService.Core.UnitTests;

public class DistributeWeeklyRebateBonusTests
{
	[Fact]
	public async Task 輸入日期_發放每週反水()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new DistributeWeeklyRebateBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider);

		var command = new DistributeWeeklyRebateBonusCommand(DateOnly.Parse("2022-12-12"));

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebateWeekly"))
			.Returns(new[]
			{
				new PromotionMainInfo()
				{
					Id = Guid.NewGuid(),
					BrandId= Guid.NewGuid(),
					EventDuring = new During(DateTime.Parse("2022-12-1"), DateTime.Parse("2023-1-1"))
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionRebateSettlementsAsync(
				Arg.Any<Guid>(),
				 Arg.Is(DateOnly.Parse("2022-12-4")),
				 Arg.Is(DateOnly.Parse("2022-12-12")));
	}

	[Fact]
	public async Task 每週反水發放_觸發時間未到不會發放()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new DistributeWeeklyRebateBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider);

		var command = new DistributeWeeklyRebateBonusCommand(DateOnly.Parse("2022-12-3"));

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebateWeekly"))
			.Returns(new[]
			{
				new PromotionMainInfo()
				{
					Id = Guid.NewGuid(),
					BrandId= Guid.NewGuid(),
					EventDuring = new During(DateTime.Parse("2022-12-1"), DateTime.Parse("2023-1-1"))
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(0)
			.QueryPromotionRebateSettlementsAsync(
				Arg.Any<Guid>(),
				 Arg.Any<DateOnly>(),
				 Arg.Any<DateOnly>());
	}

	[Fact]
	public async Task 每週反水發放_第一個週期如果不是滿週期會從開始日計算()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new DistributeWeeklyRebateBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider);

		var command = new DistributeWeeklyRebateBonusCommand(DateOnly.Parse("2022-12-5"));

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebateWeekly"))
			.Returns(new[]
			{
				new PromotionMainInfo()
				{
					Id = Guid.NewGuid(),
					BrandId= Guid.NewGuid(),
					EventDuring = new During(DateTime.Parse("2022-12-1"), DateTime.Parse("2023-1-1"))
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionRebateSettlementsAsync(
				Arg.Any<Guid>(),
				 Arg.Is(DateOnly.Parse("2022-12-1")),
				 Arg.Is(DateOnly.Parse("2022-12-5")));
	}

	[Fact]
	public async Task 每週反水發放_如果活動結束日不是結算日會以結束日計算()
	{
		// Arrange
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakeServiceProvider = Substitute.For<IServiceProvider>();

		var sut = new DistributeWeeklyRebateBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider);

		var command = new DistributeWeeklyRebateBonusCommand(DateOnly.Parse("2022-12-10"));

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebateWeekly"))
			.Returns(new[]
			{
				new PromotionMainInfo()
				{
					Id = Guid.NewGuid(),
					BrandId= Guid.NewGuid(),
					EventDuring = new During(DateTime.Parse("2022-12-1"), DateTime.Parse("2022-12-10"))
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		var fakeMessageSender = Substitute.For<IMessageSender>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(IMessageSender)))
			.Returns(fakeMessageSender);

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionRebateSettlementsAsync(
				Arg.Any<Guid>(),
				 Arg.Is(DateOnly.Parse("2022-12-4")),
				 Arg.Is(DateOnly.Parse("2022-12-10")));
	}
}
