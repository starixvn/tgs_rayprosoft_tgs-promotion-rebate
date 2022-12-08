using NSubstitute;
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

		var sut = new DistributeDailyRebateBonusCommandService(
			fakePromotionServiceClient,
			fakeTgsRepository,
			fakeServiceProvider);

		var command = new DistributeDailyRebateBonusCommand(DateOnly.Parse("2022-12-8"));

		_ = fakePromotionServiceClient.ListPromotionsByPromotionTypeAsync(Arg.Is("TurnoverRebate"))
			.Returns(new[]
			{
				new PromotionMainInfo()
				{
					Id = Guid.NewGuid(),
					BrandId= Guid.NewGuid(),
					EventDuring = new During(DateTime.Parse("2022-12-5"), DateTime.Parse("2022-12-10"))
				}
			}.ToAsyncEnumerable());

		var fakeSerialNumberGenerator = Substitute.For<ISerialNumberGenerator>();
		_ = fakeServiceProvider.GetService(Arg.Is(typeof(ISerialNumberGenerator)))
			.Returns(fakeSerialNumberGenerator);

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakeTgsRepository.Received(1)
			.QueryPromotionRebateSettlementsAsync(
				Arg.Any<Guid>(),
				 Arg.Is(command.ScheduleDate.AddDays(-1)),
				 Arg.Is(command.ScheduleDate));
	}
}
