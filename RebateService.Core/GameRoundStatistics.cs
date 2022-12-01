namespace Tgs.Services.Rebate;
public record GameRoundStatistics(
    string PlayerCode,
    int VipLevel,
    Guid BrandId,
    Guid GameProviderId,
    GameType GameType,
    decimal TotalTurnover,
    DateOnly ReportDate);
