using Tgs.Promotion.Clients;
using Tgs.SerialNumberGenerator;

namespace Tgs.Services.Rebate;

internal class PromotionDistributor
{
	private readonly PromotionMainInfo _promotionMainInfo;
	private readonly DateOnly _scheduleDate;
	private readonly ISerialNumberGenerator _serialNumberGenerator;

	public PromotionDistributor(PromotionMainInfo promotionMainInfo, DateOnly scheduleDate, ISerialNumberGenerator serialNumberGenerator)
	{
		_promotionMainInfo = promotionMainInfo ?? throw new ArgumentNullException(nameof(promotionMainInfo));
		_scheduleDate = scheduleDate;
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
	}

	public async Task ProcessDistributesAsync(
		ITgsRepository tgsRepository,
		CancellationToken cancellationToken = default)
	{
		var distributes = tgsRepository.QueryPromotionRebateSettlementsAsync(
			_promotionMainInfo.Id,
			_scheduleDate.AddDays(-1),
			_scheduleDate,
			cancellationToken)
			.SelectAwait(async settle => new PromotionRebateDistribute(
				await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false),
				settle.ReportDate,
				settle.PlayerCode,
				settle.BrandId,
				_promotionMainInfo.Id,
				settle.TotalTurnover,
				settle.Bonus,
				settle.BucketAmount,
				settle.Settings))
			.ToEnumerable();

		await tgsRepository.SavePromotionRebateDistributesAsync(
			distributes,
			cancellationToken).ConfigureAwait(false);

		//await Task.WhenAll(distributes.ToAsyncEnumerable()
		//	.Select(async dist =>
		//	{
		//		var hash = dist.DistributeDate.DayNumber;

		//		var planInfo = await promotionServiceClient.GetPromotionDistributePlanAsync(
		//			_promotionMainInfo.Id,
		//			hash,
		//			cancellationToken).ConfigureAwait(false);

		//		_ = await promotionServiceClient.DistributePromotionBonusAsync(
		//			planInfo.Id,
		//			dist.PlayerCode,
		//			dist.Bonus,
		//			dist.BucketAmount,
		//			null,
		//			dist.Id,
		//			cancellationToken).ConfigureAwait(false);

		//	})
		//	.ToEnumerable()).ConfigureAwait(false);
	}
}
