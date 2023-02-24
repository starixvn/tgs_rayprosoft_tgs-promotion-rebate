using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Promotion.Clients;
using Tgs.Services.Rebate;
using Tgs.Services.Rebate.ApplicationServices;

namespace RebateService.Core.UnitTests;

public class PayOutTests
{
	[Fact]
	public async Task 發放已經計算好的反水給玩家()
	{
		// Arrange
		var fakeTgsRepository = Substitute.For<ITgsRepository>();
		var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

		var sut = new PayOutCommandService(
			fakeTgsRepository,
			fakePromotionServiceClient,
			NullLogger<PayOutCommandService>.Instance);

		var command = new PayOutCommand(Guid.NewGuid(),Guid.NewGuid());

		var distribute = new PromotionRebateDistribute(
			command.DistributeId,
			DateTime.Parse("2022-12-8"),
			DateTime.Parse("2022-12-8"),
			"test1",
			Guid.NewGuid(),
			Guid.NewGuid(),
			100M,
			10000M,
			new RebateSettings(
				null,
				null,
				0M,
				1000M,
				10M,
				100M,
				true,
				new[] { 1 }));
		_ = fakeTgsRepository.GetRebateDistributeAsync(Arg.Is(command.DistributeId))
			.Returns(distribute);

		// Act
		await sut.ExecuteAsync(command);

		// Assert
		_ = fakePromotionServiceClient.Received(1)
			.DistributePromotionBonusAsync(
				Arg.Is(command.PlanId),
				Arg.Is(distribute.PlayerCode),
				Arg.Is(distribute.Bonus),
				Arg.Is(distribute.BucketAmount),
				Arg.Any<string>(),
				Arg.Is(distribute.Id));
		_ = fakeTgsRepository.Received(1)
			.SetPromotionRebateDistributedAsync(Arg.Is(distribute.Id));
	}
}
