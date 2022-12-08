using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Services.Rebate.ApplicationServices;

internal class PayOutCommandService : ICommandService<PayOutCommand>
{
	private readonly ITgsRepository _tgsRepository;
	private readonly IPromotionServiceClient _promotionServiceClient;

	public PayOutCommandService(ITgsRepository tgsRepository, IPromotionServiceClient promotionServiceClient)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
	}

	public async ValueTask ExecuteAsync(PayOutCommand command, CancellationToken cancellationToken = default)
	{
		var distribute = await _tgsRepository.GetRebateDistributeAsync(
			command.DistributeId,
			cancellationToken).ConfigureAwait(false);

		if (distribute is null)
			return;

		var conditionHash = distribute.DistributeDate.DayNumber;
		var plan = await _promotionServiceClient.GetPromotionDistributePlanAsync(
			distribute.PromotionId,
			conditionHash,
			cancellationToken).ConfigureAwait(false);

		_ = await _promotionServiceClient.DistributePromotionBonusAsync(
			plan.Id,
			distribute.PlayerCode,
			distribute.Bonus,
			distribute.BucketAmount,
			null,
			distribute.Id,
			cancellationToken).ConfigureAwait(false);

		await _tgsRepository.SetPromotionRebateDistributedAsync(
			distribute.Id,
			cancellationToken).ConfigureAwait(false);
	}
}
