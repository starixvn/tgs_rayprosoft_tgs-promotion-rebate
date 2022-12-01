using NSubstitute;

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

        var sut = new CalculateDailyRebateCommandService(
            fakeTgsRepository,
            fakePromotionServiceClient);

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
}
