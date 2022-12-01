namespace Tgs.Services.Rebate;
public record RebateSettings(
   GameType? GameType,
   IEnumerable<Guid> GameProviderIds,
   decimal? BonusAmountLimit,
   decimal Percentage,
   decimal TurnoverMultiple,
   bool DefaultJoin,
   IEnumerable<int> VipLevels);
