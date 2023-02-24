namespace Tgs.Services.Rebate;
public record PromotionRebateDistribute(
	Guid Id,
	DateTime DistributeFrom,
	DateTime DistributeTo,
	string PlayerCode,
	Guid BrandId,
	Guid PromotionId,
	decimal Bonus,
	decimal BucketAmount,
	RebateSettings Settings);
