namespace Tgs.Services.Rebate;
public record PromotionRebateSettlement(
    DateOnly ReportDate,
    string PlayerCode,
    Guid BrandId,
    Guid PromotionId,
    decimal TotalTurnover,
    decimal Bonus,
    decimal BucketAmount,
    RebateSettings Settings);
