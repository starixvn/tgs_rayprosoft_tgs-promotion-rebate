using System.Text.Json;

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

    public IAsyncEnumerable<GameRoundStatistics> QueryGameRoundStatisticsAsync(DateOnly reportDate, CancellationToken cancellationToken = default)
    {
        var queryRange = (
            From: new DateTimeOffset(
                reportDate.ToDateTime(TimeOnly.MinValue),
                TimeSpan.FromHours(8D)),
            To: new DateTimeOffset(
                reportDate.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.FromHours(8D)));

        var query = from rebate in _dbContext.PlayerGameRebateAggregates.AsQueryable()
                    where rebate.ReportDate >= queryRange.From
                        && rebate.ReportDate < queryRange.To
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

    public async ValueTask SavePromotionRebateSettlementsAsync(IEnumerable<PromotionRebateSettlement> settlements, CancellationToken cancellationToken = default)
    {
        await _dbContext.PromotionRebateDailyStatistics.AddRangeAsync(
            settlements.Select(s => new PromotionRebateDailyStatistic
            {
                ReportDate = s.ReportDate,
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

    public IAsyncEnumerable<PlayerJoinRecord> QueryPromotionJoinPlayersAsync(IEnumerable<Guid> promotions, CancellationToken cancellationToken = default)
    {
        var queryPromotions = promotions.ToArray();

        return _dbContext.RebatePromotionJoinPlayers.AsQueryable()
            .Where(joinPlayer => queryPromotions.Contains(joinPlayer.PromotionId))
            .Select(joinPlayer => new PlayerJoinRecord(joinPlayer.PromotionId, joinPlayer.PlayerCode))
            .ToAsyncEnumerable();
    }
}
