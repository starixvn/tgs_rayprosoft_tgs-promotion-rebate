using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tgs.BrandCertificate.Clients;
using Tgs.Promotion.Clients;
using Tgs.ServiceUtility;

namespace Tgs.Services.Rebate.ApplicationServices;

internal class DistributeWeeklyRebateBonusCommandService : ICommandService<DistributeWeeklyRebateBonusCommand>
{
	private readonly IBrandServiceClient _brandServiceClient;
	private readonly ILogger<DistributeWeeklyRebateBonusCommandService> _logger;
	private readonly IPromotionClient _promotionClient;
	private readonly IPromotionServiceClient _promotionServiceClient;
	private readonly IServiceProvider _serviceProvider;
	private readonly ITgsRepository _tgsRepository;

	public DistributeWeeklyRebateBonusCommandService(
		IPromotionClient promotionClient,
		IPromotionServiceClient promotionServiceClient,
		ITgsRepository tgsRepository,
		IServiceProvider serviceProvider,
		IBrandServiceClient brandServiceClient,
		ILogger<DistributeWeeklyRebateBonusCommandService> logger)
	{
		_promotionClient =
			promotionClient ?? throw new ArgumentNullException(nameof(promotionClient));
		_promotionServiceClient =
			promotionServiceClient ?? throw new ArgumentNullException(nameof(promotionServiceClient));
		_tgsRepository = tgsRepository ?? throw new ArgumentNullException(nameof(tgsRepository));
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_brandServiceClient = brandServiceClient;
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async ValueTask ExecuteAsync(DistributeWeeklyRebateBonusCommand command,
		CancellationToken cancellationToken = default)
	{
		var hour = command.ScheduleDateTime.Hour;

		var value = await _tgsRepository.GetTimeSpanMinutesAsync("PromotionWeeklyRebateMinutes", cancellationToken);

		if (value == null)
		{
			_logger.LogError(@"Settings key ""PromotionWeeklyRebateMinutes"" is null.");

			return;
		}

		var timeSpanMinutesJson = JsonSerializer.Deserialize<Dictionary<int, int?>>(value);


		if (timeSpanMinutesJson?.TryGetValue(hour, out var timeSpanMinutes) == null || timeSpanMinutes == null)
		{
			_logger.LogInformation("Can't found the timeSpanMinutes value from json. (PromotionWeeklyRebateMinutes)");

			return;
		}

		var scheduleDateTime = new DateTimeOffset(command.ScheduleDateTime)
			.ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value)).AddDays(-1);
		var weekFrom = scheduleDateTime.AddDays(-7).GetMonday();
		var weekTo = scheduleDateTime.AddDays(-7).GetSunday();

		var brandIds = await _brandServiceClient
			.GetBrandIdsByTimeSpanMinutesAsync(timeSpanMinutes!.Value, cancellationToken)
			.ToArrayAsync(cancellationToken);

		var promotions = await _promotionServiceClient
			.ListPromotionsByPromotionTypeAsync("TurnoverRebateWeekly", cancellationToken)
			.Where(x => brandIds.Contains(x.BrandId))
			.Where(x => x.EventDuring.From >= weekFrom.UtcDateTime && x.EventDuring.From <= weekTo.UtcDateTime ||
				x.EventDuring.To >= weekFrom.UtcDateTime && x.EventDuring.To <= weekTo.UtcDateTime ||
				x.EventDuring.From < weekFrom.UtcDateTime && x.EventDuring.To > weekTo.UtcDateTime
			)
			.SelectAwait(async info =>
			{
				var promotionPaged = await _promotionClient.GetPromotionsAsync(new Tgs.Promotion.Clients.Models.Promotion.GetPromotionsRequest { BrandId = p.Info.BrandId, Status = 0, ShowAll = true });
				var promotion = promotionPaged.Items.First(x => x.Id == info.Id);

				var startTime =
					new DateTimeOffset(info.EventDuring.From).ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value));
				var endTime =
					new DateTimeOffset(info.EventDuring.To).ToOffset(TimeSpan.FromMinutes(timeSpanMinutes!.Value));

				return (
					Info: info,
					PreviousTriggerDate: weekFrom < startTime ? startTime : weekFrom,
					NextTriggerDate: weekTo > endTime ? endTime : weekTo,
					promotion.AmountUsed);
			})
			.ToArrayAsync(cancellationToken)
			.ConfigureAwait(false);

		var distributors = promotions.ToAsyncEnumerable()
			.SelectAwait(async p =>
			{
				var promotionPaged = await _promotionClient.GetPromotionsAsync(new Tgs.Promotion.Clients.Models.Promotion.GetPromotionsRequest { BrandId = p.Info.BrandId, Status = 0, ShowAll = true });
				var promotion = promotionPaged.Items.First(x => x.Id == p.Info.Id);

				return ActivatorUtilities.CreateInstance<PromotionDistributor>(
					_serviceProvider,
					p.Info,
					p.PreviousTriggerDate,
					p.NextTriggerDate,
					promotion.AmountUsed,
					promotion.Unlimited);
			});

		await foreach (var distributor in distributors.ConfigureAwait(false))
			await distributor.ProcessDistributesAsync(
				_tgsRepository,
				cancellationToken).ConfigureAwait(false);
	}
}