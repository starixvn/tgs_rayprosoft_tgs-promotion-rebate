using Google.Protobuf;
using Tgs.MessageQueue;
using Tgs.Promotion.Clients;
using Tgs.SerialNumberGenerator;
using Tgs.Services.Rebate.Protos;

namespace Tgs.Services.Rebate;

internal class PromotionDistributor
{
	private readonly PromotionMainInfo _promotionMainInfo;
	private readonly DateOnly _startDate;
	private readonly DateOnly _endDate;
	private readonly ISerialNumberGenerator _serialNumberGenerator;
	private readonly IMessageSender _messageSender;

	public PromotionDistributor(
		PromotionMainInfo promotionMainInfo,
		DateOnly startDate,
		DateOnly endDate,
		ISerialNumberGenerator serialNumberGenerator,
		IMessageSender messageSender)
	{
		_promotionMainInfo = promotionMainInfo ?? throw new ArgumentNullException(nameof(promotionMainInfo));
		_startDate = startDate;
		_endDate = endDate;
		_serialNumberGenerator = serialNumberGenerator ?? throw new ArgumentNullException(nameof(serialNumberGenerator));
		_messageSender = messageSender ?? throw new ArgumentNullException(nameof(messageSender));
	}

	public async Task ProcessDistributesAsync(
		ITgsRepository tgsRepository,
		CancellationToken cancellationToken = default)
	{
		var distributes = tgsRepository.QueryPromotionRebateSettlementsAsync(
			_promotionMainInfo.Id,
			_startDate,
			_endDate,
			cancellationToken)
			.GroupBy(settle => settle.PlayerCode)
			.SelectAwait(async g => new
			{
				ReportDate = _endDate.AddDays(-1),
				PlayerCode = g.Key,
				_promotionMainInfo.BrandId,
				PromotionId = _promotionMainInfo.Id,
				TotalTurnover = await g.SumAsync(
					s => s.TotalTurnover, cancellationToken).ConfigureAwait(false),
				(await g.OrderByDescending(
					s => s.ReportDate)
					.FirstAsync(cancellationToken)
					.ConfigureAwait(false)).Settings

			})
			.Where(sum => sum.TotalTurnover >= sum.Settings.TurnoverThreshold)
			.Select(sum => new
			{
				sum.ReportDate,
				sum.PlayerCode,
				sum.BrandId,
				sum.PromotionId,
				sum.TotalTurnover,
				sum.Settings,
				CalculateBonus = sum.TotalTurnover * sum.Settings.Percentage / 100M
			})
			.SelectAwait(async sum => sum.CalculateBonus > sum.Settings.BonusAmountLimit
				? new PromotionRebateDistribute(
					await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false),
					sum.ReportDate,
					sum.PlayerCode,
					sum.BrandId,
					_promotionMainInfo.Id,
					sum.Settings.BonusAmountLimit,
					sum.Settings.BonusAmountLimit * sum.Settings.TurnoverMultiple,
					sum.Settings)
				: new PromotionRebateDistribute(
					await _serialNumberGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false),
					sum.ReportDate,
					sum.PlayerCode,
					sum.BrandId,
					_promotionMainInfo.Id,
					sum.CalculateBonus,
					sum.CalculateBonus * sum.Settings.TurnoverMultiple,
					sum.Settings))
			.ToEnumerable()
			.ToArray();

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
