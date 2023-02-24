namespace Tgs.Services.Rebate;
public record PromotionRebateSettlement(
	string PlayerCode,
	Guid BrandId,
	Guid PromotionId,
	decimal TotalTurnover,
	decimal Bonus,
	decimal BucketAmount,
	RebateSettings Settings);
