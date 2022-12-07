using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Services.Rebate.ApplicationServices;

file static class ExtensionMethods
{
    internal static DateOnly ToDateOnly(this DateTime date)
        => DateOnly.FromDateTime(new DateTimeOffset(date).ToOffset(TimeSpan.FromHours(8D)).DateTime);

    internal static bool Between(this DateOnly date, DateOnly from, DateOnly to)
        => from >= date && to <= date;
}

internal class CalculateDailyRebateCommandService : ICommandService<CalculateDailyRebateCommand>
{
    private readonly ITgsRepository _tgsRepository;
    private readonly IPromotionServiceClient _promotionServiceClient;

    public CalculateDailyRebateCommandService(ITgsRepository tgsRepository, IPromotionServiceClient promotionServiceClient)
    {
        _tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
        _promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
    }

    public async ValueTask ExecuteAsync(CalculateDailyRebateCommand command, CancellationToken cancellationToken = default)
    {
        var statistics = await _tgsRepository
            .QueryGameRoundStatisticsAsync(command.ReportDate, cancellationToken)
            .ToLookupAsync(s => s.BrandId, cancellationToken)
            .ConfigureAwait(false)!;

        var promotions = await _promotionServiceClient
            .ListPromotionsByPromotionTypeAsync("TurnoverRebate", cancellationToken)
            .Where(info => command.ReportDate.Between(
                info.EventDuring.From.ToDateOnly(),
                info.EventDuring.To.ToDateOnly()))
            .Where(info => statistics.Contains(info.BrandId))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false)!;

        var playerJoinList = await _tgsRepository
            .QueryPromotionJoinPlayersAsync(promotions.Select(p => p.Id), cancellationToken)
            .ToLookupAsync(j => j.PromotionId, cancellationToken)
            .ConfigureAwait(false)!;

        var settlements = promotions.ToAsyncEnumerable()
            .Select(info => new PromotionCounter(
                info,
                _promotionServiceClient,
                playerJoinList[info.Id].Select(j => j.PlayerCode)))
            .Do(counter => counter.InitialAsync(cancellationToken))
            .SelectMany(counter => counter.ProcessBatchAsync(statistics[counter.BrandId], cancellationToken));

        await _tgsRepository.SavePromotionRebateSettlementsAsync(
            settlements.ToEnumerable(),
            cancellationToken).ConfigureAwait(false);
    }
}
