using Microsoft.Extensions.DependencyInjection;
using RebateService.Repositories.Entities;
using Tgs.Promotion.Clients;
using Tgs.Promotion.RebateService.Client;

namespace Tgs.Promotion.RebateService.IntegrationTest;

public class ManualTriggerTest
{
	[Fact]
	public async Task Settle()
	{
		await using var web = new TestPromotionApplication();
		var httpClient = web.CreateClient();

		var sut = new PromotionRebateClient(httpClient);

		await sut.SettleAsync(DateOnly.Parse("2022-01-01"), 16);
	}

	[Fact]
	public async Task Bonus()
	{
		await using var web = new TestPromotionApplication();
		var httpClient = web.CreateClient();

		var sut = new PromotionRebateClient(httpClient);

		await sut.SendBonusAsync(DateOnly.Parse("2022-01-01"), 16);
	}

}