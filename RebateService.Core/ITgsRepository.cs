namespace Tgs.Services.Rebate;

public interface ITgsRepository
{
	IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsAsync(DateTimeOffset reportDate, Guid[] brandIds,
		CancellationToken cancellationToken = default);

	ValueTask SavePromotionRebateSettlementsAsync(DateTimeOffset reportDate,
		IEnumerable<PromotionRebateSettlement> settlements, CancellationToken cancellationToken = default);

	IAsyncEnumerable<PromotionRebateSettlement> QueryPromotionRebateSettlementsAsync(
		Guid promotionId,
		DateTime startDate,
		DateTime endDate,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionRebateDistribute?> GetRebateDistributeAsync(Guid distributeId,
		CancellationToken cancellationToken = default);

	ValueTask<PromotionRebateDistribute[]> GetRebateUnDistributedAsync(Guid promotionId, DateOnly date,
		CancellationToken cancellationToken = default);

	ValueTask<bool> CheckHaveRebateDistributeAsync(Guid promotionId, DateOnly date,
		CancellationToken cancellationToken = default);

	IAsyncEnumerable<PlayerJoinRecord> QueryPromotionJoinPlayersAsync(IEnumerable<Guid> promotions,
		CancellationToken cancellationToken = default);

	ValueTask SavePromotionRebateDistributesAsync(IEnumerable<PromotionRebateDistribute> distributes,
		CancellationToken cancellationToken = default);

	ValueTask SetPromotionRebateDistributedAsync(Guid distributeId, CancellationToken cancellationToken = default);

	ValueTask<string?> GetTimeSpanMinutesAsync(string key, CancellationToken cancellationToken = default);
}