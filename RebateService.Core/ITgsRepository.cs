namespace Tgs.Services.Rebate;

public interface ITgsRepository
{
	IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsAsync(DateOnly reportDate, CancellationToken cancellationToken = default);

	ValueTask SavePromotionRebateSettlementsAsync(IEnumerable<PromotionRebateSettlement> settlements, CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionRebateSettlement> QueryPromotionRebateSettlementsAsync(
		Guid promotionId,
		DateOnly startDate,
		DateOnly endDate,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionRebateDistribute?> GetRebateDistributeAsync(Guid distributeId, CancellationToken cancellationToken = default);

	IAsyncEnumerable<PlayerJoinRecord> QueryPromotionJoinPlayersAsync(IEnumerable<Guid> promotions, CancellationToken cancellationToken = default);

	ValueTask SavePromotionRebateDistributesAsync(IEnumerable<PromotionRebateDistribute> distributes, CancellationToken cancellationToken = default);

	ValueTask SetPromotionRebateDistributedAsync(Guid distributeId, CancellationToken cancellationToken = default);
}
