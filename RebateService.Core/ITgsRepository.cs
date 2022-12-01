namespace Tgs.Services.Rebate;

public interface ITgsRepository
{
    IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsAsync(DateOnly reportDate, CancellationToken cancellationToken = default);

    ValueTask SavePromotionRebateSettlementsAsync(IEnumerable<PromotionRebateSettlement> settlements, CancellationToken cancellationToken = default);

    IAsyncEnumerable<PlayerJoinRecord> QueryPromotionJoinPlayersAsync(IEnumerable<Guid> promotions, CancellationToken cancellationToken = default);
}
