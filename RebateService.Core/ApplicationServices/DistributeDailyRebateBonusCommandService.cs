using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Services.Rebate.ApplicationServices;

internal class DistributeDailyRebateBonusCommandService : ICommandService<DistributeDailyRebateBonusCommand>
{
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly ITgsRepository _tgsRepository;
	private readonly IServiceProvider _serviceProvider;
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly ILogger<DistributeDailyRebateBonusCommandService> _logger;

	public DistributeDailyRebateBonusCommandService(
		IPromotionServiceClient promotionServiceClient,
		ITgsRepository tgsRepository,
		IServiceProvider serviceProvider,
		IBrandServiceClient brandServiceClient,
		ILogger<DistributeDailyRebateBonusCommandService> logger)
	{
		_promotionServiceClient = promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_brandServiceClient = brandServiceClient ?? throw new ArgumentNullException(nameof(brandServiceClient));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(DistributeDailyRebateBonusCommand command, CancellationToken cancellationToken = default)
	{
		var hour = command.ScheduleDateTime.Hour;

		var value = await _tgsRepository.GetTimeSpanMinutesAsync("PromotionDailyRebateMinutes", cancellationToken);

		if (value == null)
		{
			_logger.LogError(@"Settings key ""PromotionDailyRebateMinutes"" is null.");

			return;
		}

		var timeSpanMinutesJson = JsonSerializer.Deserialize<Dictionary<int, int?>>(value);


		if (timeSpanMinutesJson?.TryGetValue(hour, out var timeSpanMinutes) == null || timeSpanMinutes == null)
		{
			_logger.LogWarning("Can't found the timeSpanMinutes value from json. (PromotionDailyRebateMinutes)");

			return;
		}

		var reportDateTime = new DateTimeOffset(command.ScheduleDateTime).ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value)).AddDays(-1);

		var brandIds = await _brandServiceClient.GetBrandIdsByTimeSpanMinutesAsync(timeSpanMinutes!.Value, cancellationToken).ToArrayAsync(cancellationToken);

		var promotions = await _promotionServiceClient
			.ListPromotionsByPromotionTypeAsync("TurnoverRebate", cancellationToken)
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.EventDuring.From <= reportDateTime.UtcDateTime && x.EventDuring.To >= reportDateTime.UtcDateTime)
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var distributors = promotions.ToAsyncEnumerable()
			.SelectAwait(async info =>
			{
				var promotion = await _promotionServiceClient.ListPromotionsAsync(info.BrandId, cancellationToken).FirstAsync(x => x.Id == info.Id, cancellationToken: cancellationToken);

				return ActivatorUtilities.CreateInstance<PromotionDistributor>(
					_serviceProvider,
					info,
					reportDateTime.UtcDateTime,
					reportDateTime.UtcDateTime,
					promotion.UsedAmount);
			});

		await foreach (var distributor in distributors.ConfigureAwait(false))
		{
			await distributor.ProcessDistributesAsync(
				_tgsRepository,
				cancellationToken).ConfigureAwait(false);
		}
	}
}
