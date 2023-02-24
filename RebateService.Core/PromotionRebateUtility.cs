using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tgs.Services.Rebate;

public static class PromotionRebateUtility
{
	public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
	{
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		WriteIndented = false,
		Converters = { new JsonStringEnumConverter() }
	};
}
