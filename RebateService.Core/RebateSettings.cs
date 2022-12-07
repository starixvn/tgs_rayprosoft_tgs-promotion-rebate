using System.Text.Json.Serialization;

namespace Tgs.Services.Rebate;
public record RebateSettings(
   GameType? GameType,
   IEnumerable<Guid>? GameProviderIds,
   [property: JsonPropertyName("turnoverLimit")]
   decimal TurnoverThreshold,
   decimal BonusAmountLimit,
   decimal Percentage,
   decimal TurnoverMultiple,
   [property: JsonPropertyName("playerAutoJoin")]
   bool DefaultJoin,
   IEnumerable<int> VipLevels);
