namespace Tgs.Services.Rebate;
public record PromotionRebateDistribute(
	Guid Id,
	DateOnly DistributeDate,
	string PlayerCode,
	Guid BrandId,
	Guid PromotionId,
	decimal TotalTurnover,
	decimal Bonus,
	decimal BucketAmount,
	RebateSettings Settings);
