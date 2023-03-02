namespace Tgs.Promotion.RebateService.Client;

public interface IPromotionRebateClient
{
	Task SettleAsync(DateOnly reportDate, int utcHour);

	Task SendBonusAsync(DateOnly reportDate, int utcHour);
}
