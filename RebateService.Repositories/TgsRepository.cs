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

		var query = from rebate in _dbContext.PlayerGameRebateAggregates.AsQueryable()
					where rebate.ReportDateTime >= From.UtcDateTime
						&& rebate.ReportDateTime < To.UtcDateTime
						&& brandIds.Contains(rebate.BrandId)
					join brandGame in _dbContext.BrandGames on rebate.GameId equals brandGame.GameId
					into groupJoin
					from brandGame in groupJoin.DefaultIfEmpty()
					where brandGame == null || brandGame.Rebate
					group rebate.Turnover by new
					{
						rebate.PlayerCode,
						rebate.VipLevel,
						rebate.BrandId,
						rebate.GameType,
						rebate.GameProviderId,
						rebate.ReportDate
					} into g
					select new
					{
						g.Key.PlayerCode,
						VipLevel = g.Key.VipLevel!.Value,
						g.Key.BrandId,
						g.Key.GameProviderId,
						g.Key.GameType,
						TotalTurnover = g.Sum(),
						g.Key.ReportDate
					};

		return query.ToAsyncEnumerable()
			.Select(row => new GameRoundStatistics(
				row.PlayerCode,
				row.VipLevel,
				row.BrandId,
				row.GameProviderId,
				(GameType)row.GameType,
				row.TotalTurnover,
				DateOnly.FromDateTime(new DateTimeOffset(row.ReportDate)
					.ToOffset(TimeSpan.FromHours(8D))
					.Date)));
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

		return _dbContext.RebatePromotionJoinPlayers.AsQueryable()
			.Where(joinPlayer => queryPromotions.Contains(joinPlayer.PromotionId))
			.Select(joinPlayer => new PlayerJoinRecord(joinPlayer.PromotionId, joinPlayer.PlayerCode))
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
