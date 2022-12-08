using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.SerialNumberGenerator;
using Tgs.Services.Rebate.Protos;

namespace Tgs.Services.Rebate;

internal class PromotionDistributor
{
	private readonly PromotionMainInfo _promotionMainInfo;
	private readonly DateOnly _scheduleDate;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly IMessageSender _messageSender;

	public PromotionDistributor(
		PromotionMainInfo promotionMainInfo,
		DateOnly scheduleDate,
		ISerialNumberGenerator serialNumberGenerator,
		IMessageSender messageSender)
	{
		_promotionMainInfo = promotionMainInfo ?? throw new ArgumentNullException(nameof(promotionMainInfo));
		_scheduleDate = scheduleDate;
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
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
				settle.Bonus,
				settle.BucketAmount,
				settle.Settings))
			.ToEnumerable();

		await tgsRepository.SavePromotionRebateDistributesAsync(
			distributes,
			cancellationToken).ConfigureAwait(false);

		await foreach (var dist in distributes.ToAsyncEnumerable()
			.ConfigureAwait(false)
			.WithCancellation(cancellationToken))
		{
			var request = new BonusTransferRequest
			{
				DistributeId = dist.Id
			};

			await _messageSender.PublishAsync(
				"tgs.rebate.ipc.payout",
				request.ToByteArray(),
				cancellationToken).ConfigureAwait(false);
		}
	}
}
