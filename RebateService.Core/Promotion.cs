namespace Tgs.Services.Rebate;

public record Promotion(
	Guid Id,
	Guid BrandId,
	string Name,
	(DateTime From, DateTime To) EventDuring,
	decimal? PromotionLimitAmount,
	bool IsAutoSent,
	int[] ApplyVipLevels,
	RebateSettings Settings);