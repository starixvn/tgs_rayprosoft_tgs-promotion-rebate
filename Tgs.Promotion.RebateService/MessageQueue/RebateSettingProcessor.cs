using System.Text.Json;

using Google.Protobuf;

using Tgs.MessageQueue;
using Tgs.Promotion.Clients.Protos;
using Tgs.Services.Rebate;

namespace Tgs.Promotion.RebateService.MessageQueue;

public class RebateSettingProcessor : IMessageProcessor
{
	public ValueTask<ReadOnlyMemory<byte>> HandleAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
	{
		var request = CampaignDetailRequest.Parser.ParseFrom(data.Span);

		var settings = JsonSerializer.Deserialize<RebateSettings>(request.Settings, PromotionUtility.SerializerOptions);

		if (settings is null)
			throw new InvalidOperationException("Settings is null");

		var response = new CampaignDetailResponse
		{
			CampaignDetail = JsonSerializer.Serialize(settings, PromotionUtility.SerializerOptions),
			BonusAmountLimit = settings.BonusAmountLimit,
			GameType = settings.GameType?.ToString() ?? string.Empty,
			Percentage = settings.Percentage,
			TurnoverMultiple = settings.TurnoverMultiple
		};

		return ValueTask.FromResult((ReadOnlyMemory<byte>)response.ToByteArray());
	}
}
