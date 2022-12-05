using Tgs.Promotion.Clients;

namespace Tgs.Services.Rebate;

internal class PromotionCounter
{
    private readonly PromotionMainInfo _promotionMainInfo;
    private readonly IPromotionServiceClient _promotionServiceClient;
    private readonly HashSet<string> _joinPlayers;
    private RebateSettings _rebateSettings = default!;
    private int _gameProviderIdCount;

    public Guid BrandId => _promotionMainInfo.BrandId;

    public PromotionCounter(
        PromotionMainInfo promotionMainInfo,
        IPromotionServiceClient promotionServiceClient,
        IEnumerable<string> joinPlayers)
    {
        _promotionMainInfo = promotionMainInfo ?? throw new ArgumentNullException(nameof(promotionMainInfo));
        _promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
        _joinPlayers = joinPlayers.ToHashSet();
    }

    public async Task InitialAsync(CancellationToken cancellationToken = default)
    {
        var promotionRule = await _promotionServiceClient.GetPromotionRuleAsync<PromotionRule>(
            _promotionMainInfo.Id,
            cancellationToken).ConfigureAwait(false);

        if (promotionRule is null)
            throw new NullReferenceException(nameof(promotionRule));

        _rebateSettings = promotionRule.CampaignDetail;
        _gameProviderIdCount = _rebateSettings.GameProviderIds.Count();
    }

    public IAsyncEnumerable<PromotionRebateSettlement> ProcessBatchAsync(
        IEnumerable<GameRoundStatistics> statistics,
        CancellationToken cancellationToken = default)
        => (from gameRound in statistics
            where gameRound.BrandId == BrandId
            where _rebateSettings.DefaultJoin
                || _joinPlayers.Contains(gameRound.PlayerCode)
            where _rebateSettings.VipLevels.Contains(gameRound.VipLevel)
            where _rebateSettings.GameType is null
                || gameRound.GameType == _rebateSettings.GameType
            where _gameProviderIdCount == 0
                || _rebateSettings.GameProviderIds.Contains(gameRound.GameProviderId)
            group gameRound by new { gameRound.ReportDate, gameRound.PlayerCode } into g
            let totalTurnover = g.Sum(round => round.TotalTurnover)
            let calculateBonus = totalTurnover * _rebateSettings.Percentage / 100M
            let bonus = calculateBonus
            select new PromotionRebateSettlement(
                g.Key.ReportDate,
                g.Key.PlayerCode,
                BrandId,
                _promotionMainInfo.Id,
                totalTurnover,
                bonus,
                _rebateSettings.TurnoverMultiple * bonus,
                _rebateSettings))
            .ToAsyncEnumerable()
            .Do(_ => cancellationToken.ThrowIfCancellationRequested());

    internal record PromotionRule(RebateSettings CampaignDetail);
}
