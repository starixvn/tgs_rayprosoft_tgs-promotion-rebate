using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Services.Rebate.ApplicationServices;

internal class CalculateDailyRebateCommandService : ICommandService<CalculateDailyRebateCommand>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly ILogger<CalculateDailyRebateCommandService> _logger;
	private readonly IPromotionClient _promotionClient;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly ITgsRepository _tgsRepository;

	public CalculateDailyRebateCommandService(
		ITgsRepository tgsRepository,
		IPromotionClient promotionClient,
		IPromotionServiceClient promotionServiceClient,
		IBrandServiceClient brandServiceClient,
		ILogger<CalculateDailyRebateCommandService> logger)
	{
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_promotionClient = promotionClient ?? throw new ArgumentNullException(nameof(promotionClient));
		_promotionServiceClient =
			promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(CalculateDailyRebateCommand command,
		CancellationToken cancellationToken = default)
	{
		var hour = command.SettleDateTime.Hour;

		var value = await _tgsRepository.GetTimeSpanMinutesAsync("PromotionDailySettleMinutes", cancellationToken);

		if (value == null)
		{
			_logger.LogError(@"Settings key ""PromotionDailySettleMinutes"" is null.");

			return;
		}

		var timeSpanMinutesJson = JsonSerializer.Deserialize<Dictionary<int, int?>>(value);

		if (timeSpanMinutesJson?.TryGetValue(hour, out var timeSpanMinutes) == null || timeSpanMinutes == null)
		{
			_logger.LogWarning("Can't found the timeSpanMinutes value from json. (PromotionDailySettleMinutes)");

			return;
		}

		var reportDate = new DateTimeOffset(command.SettleDateTime)
			.ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value)).AddDays(-1);

		var brandIds = await _brandServiceClient
			.GetBrandIdsByTimeSpanMinutesAsync(timeSpanMinutes!.Value, cancellationToken)
			.ToArrayAsync(cancellationToken);

		var statistics = await _tgsRepository
			.QueryGameRoundStatisticsAsync(reportDate, brandIds, cancellationToken)
			.Do(record => _logger.LogDebug("GameRound: {record}", record))
			.ToLookupAsync(s => s.BrandId, cancellationToken)
			.ConfigureAwait(false);

		var promotions = await new[]
			{
				_promotionServiceClient
					.ListPromotionsByPromotionTypeAsync("TurnoverRebate", cancellationToken),
				_promotionServiceClient
					.ListPromotionsByPromotionTypeAsync("TurnoverRebateWeekly", cancellationToken)
			}.ToAsyncEnumerable()
			.Merge()
			.Do(info => _logger.LogDebug("Promotion: {info}", info))
			.Where(x => x.EventDuring.From <= reportDate.UtcDateTime && x.EventDuring.To >= reportDate.UtcDateTime)
			.Where(info => statistics.Contains(info.BrandId))
			.Do(info => _logger.LogDebug("Match Promotion: {info}", info))
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var playerJoinList = await _tgsRepository
			.QueryPromotionJoinPlayersAsync(promotions.Select(p => p.Id), cancellationToken)
			.ToLookupAsync(j => j.PromotionId, cancellationToken)
			.ConfigureAwait(false);

		var settlements = promotions.ToAsyncEnumerable()
			.Select(info => new PromotionCounter(
				info,
				_promotionClient,
				_promotionServiceClient,
				playerJoinList[info.Id].Select(j => j.PlayerCode)))
			.Do(counter => counter.InitialAsync(cancellationToken))
			.SelectMany(counter => counter.ProcessBatchAsync(statistics[counter.BrandId], cancellationToken));

		await _tgsRepository.SavePromotionRebateSettlementsAsync(reportDate, settlements.ToEnumerable(),
			cancellationToken).ConfigureAwait(false);
	}
}