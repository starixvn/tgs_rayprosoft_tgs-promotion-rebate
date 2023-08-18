using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RebateService.Repositories.Entities;

using Tgs.Services.Rebate;

namespace RebateService.Repositories;

internal class TgsRepository : ITgsRepository
{
	private readonly TgsContext _dbContext;

	public TgsRepository(TgsContext dbContext)
	{
		_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
	}

	public IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsAsync(DateTimeOffset reportDate, Guid[] brandIds, CancellationToken cancellationToken = default)
	{
		var (From, To) = (reportDate, reportDate.AddDays(1));

		var query = _dbContext.PlayerGameRebateAggregates
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.ReportDateTime >= From.UtcDateTime
				&& x.ReportDateTime < To.UtcDateTime)
			.GroupJoin(_dbContext.BrandGames,
				aggregate => new
				{
					aggregate.BrandId,
					aggregate.GameId
				},
				brandGame => new
				{
					brandGame.BrandId,
					brandGame.GameId
				},
				(aggregate, brandGame) => new
				{
					aggregate,
					brandGame
				})
			.SelectMany(x => x.brandGame.DefaultIfEmpty(),
				(x, brandGame) => new
				{
					x.aggregate,
					brandGame
				})
			.Where(x => x.brandGame == null || x.brandGame.Rebate)
			.GroupBy(x => new
			{
				x.aggregate.BrandId,
				x.aggregate.PlayerCode,
				x.aggregate.GameType,
				x.aggregate.GameProviderId,
				x.aggregate.VipLevel
			}, (player, list) => new
			{
				player.BrandId,
				player.PlayerCode,
				player.GameType,
				VipLevel = player.VipLevel!.Value,
				player.GameProviderId,
				Turnover = list.Sum(l => l.aggregate.Turnover)
			});

		return query.ToAsyncEnumerable()
			.Select(row => new GameRoundStatistics(
				row.PlayerCode,
				row.VipLevel,
				row.BrandId,
				row.GameProviderId,
				(GameType)row.GameType,
				row.Turnover));
	}

	public IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsFromGameRoundAggregateAsync(DateTimeOffset reportDate, Guid brandId,
		CancellationToken cancellationToken = default)
	{
		var (from, to) = (reportDate, reportDate.AddDays(1));

		var query = _dbContext.GameTypeRoundAggregates
			.Where(x => x.BrandId == brandId)
			.Where(x => x.From >= from.UtcDateTime
				&& x.From < to.UtcDateTime)
			.GroupBy(x => new
			{
				x.BrandId,
				x.PlayerCode,
				x.GameType,
				x.GameProviderId,
				x.VipLevel
			}, (player, list) => new
			{
				player.BrandId,
				player.PlayerCode,
				player.GameType,
				player.VipLevel,
				player.GameProviderId,
				Turnover = list.Sum(l => l.Turnover)
			});

		return query.ToAsyncEnumerable()
			.Select(row => new GameRoundStatistics(
				row.PlayerCode,
				row.VipLevel,
				row.BrandId,
				row.GameProviderId,
				(GameType)row.GameType,
				row.Turnover));
	}

	public async ValueTask SavePromotionRebateSettlementsAsync(DateTimeOffset reportDate,
		IEnumerable<PromotionRebateSettlement> settlements, CancellationToken cancellationToken = default)
	{
		await _dbContext.PromotionRebateDailyStatistics.AddRangeAsync(
			settlements.Select(s => new PromotionRebateDailyStatistic
			{
				ReportDate = new DateOnly(reportDate.Year, reportDate.Month, reportDate.Day),
				ReportDateTime = reportDate.UtcDateTime,
				PlayerCode = s.PlayerCode,
				Bonus = s.Bonus,
				BrandId = s.BrandId,
				BucketAmount = s.BucketAmount,
				PromotionId = s.PromotionId,
				Settings = JsonSerializer.Serialize(s.Settings, PromotionRebateUtility.SerializerOptions),
				TotalTurnover = s.TotalTurnover
			}),
			cancellationToken).ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<PromotionRebateDistribute[]> GetRebateUnDistributedAsync(Guid promotionId, DateOnly date,
		CancellationToken cancellationToken = default)
		=> await _dbContext.PromotionRebateDistributeRows
			.Where(row => row.PromotionId == promotionId)
			.Where(row => row.DistributeDate == date)
			.ToAsyncEnumerable()
			.Select(row => new PromotionRebateDistribute(
				row.Id,
				row.DistributeDate,
				row.PlayerCode,
				row.BrandId,
				row.PromotionId,
				row.Bonus,
				row.BucketAmount,
				JsonSerializer.Deserialize<RebateSettings>(row.Settings, PromotionRebateUtility.SerializerOptions)!))
			.ToArrayAsync(cancellationToken);

	public async ValueTask<bool> CheckHaveRebateDistributeAsync(Guid promotionId, DateOnly date,
		CancellationToken cancellationToken = default)
		=> await _dbContext.PromotionRebateDistributeRows
			.AnyAsync(x =>
				x.PromotionId == promotionId && x.DistributeDate == date, cancellationToken: cancellationToken);

	public IAsyncEnumerable<PlayerJoinRecord> QueryPromotionJoinPlayersAsync(IEnumerable<Guid> promotions, CancellationToken cancellationToken = default)
	{
		var queryPromotions = promotions.ToArray();

		return _dbContext.PromotionParticipants
			.Where(joinPlayer => queryPromotions.Contains(joinPlayer.PromotionId))
			.Select(joinPlayer => new PlayerJoinRecord(joinPlayer.PromotionId, joinPlayer.PlayerCode!))
			.ToAsyncEnumerable();
	}

	public IAsyncEnumerable<PromotionRebateSettlement> QueryPromotionRebateSettlementsAsync(
		Guid promotionId,
		DateTime startDate,
		DateTime endDate,
		CancellationToken cancellationToken = default)
		=> _dbContext.PromotionRebateDailyStatistics.AsQueryable()
			.Where(prds => prds.PromotionId == promotionId)
			.Where(prds => prds.ReportDateTime >= startDate && prds.ReportDateTime < endDate)
			.Select(prds => new
			{
				prds.ReportDate,
				prds.PlayerCode,
				prds.BrandId,
				prds.PromotionId,
				prds.TotalTurnover,
				prds.Bonus,
				prds.BucketAmount,
				prds.Settings
			})
			.ToAsyncEnumerable()
			.Select(prds => new PromotionRebateSettlement(
				prds.PlayerCode,
				prds.BrandId,
				prds.PromotionId,
				prds.TotalTurnover,
				prds.Bonus,
				prds.BucketAmount,
				JsonSerializer.Deserialize<RebateSettings>(prds.Settings, PromotionRebateUtility.SerializerOptions)!));

	public async ValueTask SavePromotionRebateDistributesAsync(IEnumerable<PromotionRebateDistribute> distributes, CancellationToken cancellationToken = default)
	{
		var currentDate = DateTime.UtcNow;
		await _dbContext.PromotionRebateDistributeRows.AddRangeAsync(
			distributes.Select(s => new PromotionRebateDistributeRow
			{
				Id = s.Id,
				DistributeDate = s.DistributeDate,
				PlayerCode = s.PlayerCode,
				Bonus = s.Bonus,
				BrandId = s.BrandId,
				BucketAmount = s.BucketAmount,
				PromotionId = s.PromotionId,
				Settings = JsonSerializer.Serialize(s.Settings, PromotionRebateUtility.SerializerOptions),
				Status = "Create",
				CreatedOn = currentDate
			}),
			cancellationToken).ConfigureAwait(false);

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask SetPromotionRebateDistributedAsync(Guid distributeId, CancellationToken cancellationToken = default)
	{
		var row = await _dbContext.PromotionRebateDistributeRows
			.FindAsync(new object?[] { distributeId }, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		if (row is null)
			return;

		row.Status = "Distributed";

		_ = await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
	}

	public async ValueTask<string?> GetTimeSpanMinutesAsync(string key, CancellationToken cancellationToken = default)
	{
		return await _dbContext.Settings
			.Where(x => x.Key == key)
			.Select(x => x.Value)
			.FirstOrDefaultAsync(cancellationToken);

	}

	public ValueTask<PromotionRebateDistribute?> GetRebateDistributeAsync(Guid distributeId, CancellationToken cancellationToken = default)
		=> _dbContext.PromotionRebateDistributeRows
			.FindAsync(new object?[] { distributeId }, cancellationToken: cancellationToken)
			.AsTask()
			.ToAsyncEnumerable()
			.Where(row => row is not null)
			.Select(row => row!)
			.Select(row => new PromotionRebateDistribute(
				row.Id,
				row.DistributeDate,
				row.PlayerCode,
				row.BrandId,
				row.PromotionId,
				row.Bonus,
				row.BucketAmount,
				JsonSerializer.Deserialize<RebateSettings>(row.Settings, PromotionRebateUtility.SerializerOptions)!))
			.FirstOrDefaultAsync(cancellationToken);
}
