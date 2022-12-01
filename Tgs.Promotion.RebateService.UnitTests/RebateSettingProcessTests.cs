using System.Text.Json;

using Google.Protobuf;

using Tgs.Promotion.Clients.Protos;
using Tgs.Promotion.RebateService.MessageQueue;
using Tgs.Services.Rebate;

namespace Tgs.Promotion.RebateService.UnitTests;

public class RebateSettingProcessTests
{
    [Fact]
    public async Task 處理設定要求()
    {
        // Arrange
        var sut = new RebateSettingProcessor();

        var request = new CampaignDetailRequest
        {
            PromotionId = Guid.NewGuid(),
            Settings = """
                {
                    "percentage": 10,
                    "gameType": 15,
                    "gameProviderIds": [],
                    "bonusAmountLimit": 20,
                    "turnoverMultiple": 30,
                    "defaultJoin": true,
                    "vipLevels": [1],
                    "aaa": "bbb"
                }
                """
        };

        var expected = new RebateSettings(
            GameType.Slot,
            Array.Empty<Guid>(),
            20M,
            10M,
            30M,
            true,
            new[] { 1 });

        // Act
        var response = await sut.HandleAsync(request.ToByteArray());
        var actual = CampaignDetailResponse.Parser.ParseFrom(response.Span);

        // Assert
        Assert.Equal(JsonSerializer.Serialize(expected, PromotionUtility.SerializerOptions), actual.CampaignDetail);
    }
}
