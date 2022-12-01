using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;

using NSubstitute;
using NSubstitute.Core;

using Tgs.Promotion.Clients;
using Tgs.Services.Rebate;

using Xunit;

namespace RebateService.Core.UnitTests;

internal static class ExtensionMethods
{
    private static readonly Type _RuleType = typeof(PromotionCounter).GetNestedType(
        "PromotionRule",
        System.Reflection.BindingFlags.NonPublic)!;

    internal static WhenCalled<IPromotionServiceClient> WhenCallGetPromotionRuleAsync(this IPromotionServiceClient fake, Guid promotionId)
    {
        var mi = typeof(IPromotionServiceClient)
            .GetMethod(nameof(IPromotionServiceClient.GetPromotionRuleAsync))
            !.MakeGenericMethod(_RuleType);
        var parameterExp = Expression.Parameter(typeof(IPromotionServiceClient));

        var callExp = Expression.Call(
            parameterExp,
            mi,
            Expression.Constant(promotionId),
            Expression.Constant(default(CancellationToken)));

        return fake.When(Expression.Lambda<Action<IPromotionServiceClient>>(
            callExp,
            parameterExp).Compile());
    }

    internal static void ReturnRuleJson(this WhenCalled<IPromotionServiceClient> called, string ruleJson)
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());

        called.Do(callInfo => callInfo.Returns(
            Activator.CreateInstance(
                typeof(ValueTask<>).MakeGenericType(_RuleType),
                new[] {
                    JsonSerializer.Deserialize(
                        ruleJson,
                        _RuleType,
                        options)
                })));
    }
}

public class PromotionCounterTests
{
    [Fact]
    public async Task 處理活動反水_會過濾是同一個Brand的資料()
    {
        // Arrange
        var promotionMainInfo = new PromotionMainInfo
        {
            Id = Guid.NewGuid(),
            BrandId = Guid.NewGuid()
        };
        var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

        var sut = new PromotionCounter(
            promotionMainInfo,
            fakePromotionServiceClient,
            Array.Empty<string>());

        fakePromotionServiceClient
            .WhenCallGetPromotionRuleAsync(promotionMainInfo.Id)
            .ReturnRuleJson("""
                {
                    "campaignDetail": {
                        "percentage": 10,
                        "gameProviderIds": [],
                        "bonusAmountLimit": 20,
                        "turnoverMultiple": 30,
                        "defaultJoin": true,
                        "vipLevels": [1]
                    }
                }
                """);

        await sut.InitialAsync();

        var statistics = new[]
        {
            new GameRoundStatistics(
                "test1",
                1,
                promotionMainInfo.BrandId,
                Guid.NewGuid(),
                GameType.Sportsbook,
                100M,
                DateOnly.Parse("2022-11-29")),
            new GameRoundStatistics(
                "test2",
                1,
                Guid.NewGuid(),
                Guid.NewGuid(),
                GameType.Sportsbook,
                100M,
                DateOnly.Parse("2022-11-29"))
        };

        // Act
        var actual = await sut.ProcessBatchAsync(statistics).ToArrayAsync();

        // Assert
        Assert.Single(actual);
    }

    [Fact]
    public async Task 處理活動反水_如果不是預設參加_會使用參加名單過濾()
    {
        // Arrange
        var promotionMainInfo = new PromotionMainInfo
        {
            Id = Guid.NewGuid(),
            BrandId = Guid.NewGuid()
        };
        var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

        var sut = new PromotionCounter(
            promotionMainInfo,
            fakePromotionServiceClient,
            new[] { "test1" });

        fakePromotionServiceClient
            .WhenCallGetPromotionRuleAsync(promotionMainInfo.Id)
            .ReturnRuleJson("""
                {
                    "campaignDetail": {
                        "percentage": 10,
                        "gameProviderIds": [],
                        "bonusAmountLimit": 20,
                        "turnoverMultiple": 30,
                        "defaultJoin": false,
                        "vipLevels": [1]
                    }
                }
                """);

        await sut.InitialAsync();

        var statistics = new[]
        {
            new GameRoundStatistics(
                "test1",
                1,
                promotionMainInfo.BrandId,
                Guid.NewGuid(),
                GameType.Sportsbook,
                100M,
                DateOnly.Parse("2022-11-29")),
            new GameRoundStatistics(
                "test2",
                1,
                promotionMainInfo.BrandId,
                Guid.NewGuid(),
                GameType.Sportsbook,
                100M,
                DateOnly.Parse("2022-11-29"))
        };

        // Act
        var actual = await sut.ProcessBatchAsync(statistics).ToArrayAsync();

        // Assert
        Assert.Single(actual);
    }

    [Fact]
    public async Task 處理活動反水_如果GameType有值_沒有特別選定GP_會依照GameType過濾()
    {
        // Arrange
        var promotionMainInfo = new PromotionMainInfo
        {
            Id = Guid.NewGuid(),
            BrandId = Guid.NewGuid()
        };
        var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

        var sut = new PromotionCounter(
            promotionMainInfo,
            fakePromotionServiceClient,
            Array.Empty<string>());

        fakePromotionServiceClient
            .WhenCallGetPromotionRuleAsync(promotionMainInfo.Id)
            .ReturnRuleJson("""
                {
                    "campaignDetail": {
                        "percentage": 10,
                        "gameType": 15,
                        "gameProviderIds": [],
                        "bonusAmountLimit": 20,
                        "turnoverMultiple": 30,
                        "defaultJoin": true,
                        "vipLevels": [1]
                    }
                }
                """);

        await sut.InitialAsync();

        var statistics = new[]
        {
            new GameRoundStatistics(
                "test1",
                1,
                promotionMainInfo.BrandId,
                Guid.NewGuid(),
                GameType.Sportsbook,
                100M,
                DateOnly.Parse("2022-11-29")),
            new GameRoundStatistics(
                "test2",
                1,
                promotionMainInfo.BrandId,
                Guid.NewGuid(),
                GameType.Slot,
                100M,
                DateOnly.Parse("2022-11-29"))
        };

        // Act
        var actual = await sut.ProcessBatchAsync(statistics).ToArrayAsync();

        // Assert
        Assert.Single(actual);
    }

    [Fact]
    public async Task 處理活動反水_指定GameType並且選定GP_會依照GP過濾()
    {
        // Arrange
        var promotionMainInfo = new PromotionMainInfo
        {
            Id = Guid.NewGuid(),
            BrandId = Guid.NewGuid()
        };
        var targetGpId = Guid.NewGuid();

        var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

        var sut = new PromotionCounter(
            promotionMainInfo,
            fakePromotionServiceClient,
            Array.Empty<string>());

        fakePromotionServiceClient
            .WhenCallGetPromotionRuleAsync(promotionMainInfo.Id)
            .ReturnRuleJson($$"""
                {
                    "campaignDetail": {
                        "percentage": 10,
                        "gameType": 15,
                        "gameProviderIds": ["{{targetGpId}}"],
                        "bonusAmountLimit": 20,
                        "turnoverMultiple": 30,
                        "defaultJoin": true,
                        "vipLevels": [1]
                    }
                }
                """);

        await sut.InitialAsync();

        var statistics = new[]
        {
            new GameRoundStatistics(
                "test1",
                1,
                promotionMainInfo.BrandId,
                targetGpId,
                GameType.Slot,
                100M,
                DateOnly.Parse("2022-11-29")),
            new GameRoundStatistics(
                "test2",
                1,
                promotionMainInfo.BrandId,
                Guid.NewGuid(),
                GameType.Slot,
                100M,
                DateOnly.Parse("2022-11-29"))
        };

        // Act
        var actual = await sut.ProcessBatchAsync(statistics).ToArrayAsync();

        // Assert
        Assert.Single(actual);
    }

    [Fact]
    public async Task 活動反水計算會依照設定值公式計算()
    {
        // Arrange
        var promotionMainInfo = new PromotionMainInfo
        {
            Id = Guid.NewGuid(),
            BrandId = Guid.NewGuid()
        };
        var targetGpId = Guid.NewGuid();

        var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

        var sut = new PromotionCounter(
            promotionMainInfo,
            fakePromotionServiceClient,
            Array.Empty<string>());

        fakePromotionServiceClient
            .WhenCallGetPromotionRuleAsync(promotionMainInfo.Id)
            .ReturnRuleJson($$"""
                {
                    "campaignDetail": {
                        "percentage": 10,
                        "gameType": 15,
                        "gameProviderIds": ["{{targetGpId}}"],
                        "turnoverMultiple": 30,
                        "defaultJoin": true,
                        "vipLevels": [1]
                    }
                }
                """);

        await sut.InitialAsync();

        var statistics = new[]
        {
            new GameRoundStatistics(
                "test1",
                1,
                promotionMainInfo.BrandId,
                targetGpId,
                GameType.Slot,
                100M,
                DateOnly.Parse("2022-11-29"))
        };

        var expectedBonus = 100M * 10M / 100M;
        var expectedBucketAmount = expectedBonus * 30M;

        // Act
        var actual = await sut.ProcessBatchAsync(statistics).ToArrayAsync();

        // Assert
        Assert.Single(actual);
        Assert.Equal(expectedBonus, actual.First().Bonus);
        Assert.Equal(expectedBucketAmount, actual.First().BucketAmount);
    }

    [Fact]
    public async Task 活動反水計算如果Bouns超過最大發放值則以最大值為主()
    {
        // Arrange
        var promotionMainInfo = new PromotionMainInfo
        {
            Id = Guid.NewGuid(),
            BrandId = Guid.NewGuid()
        };
        var targetGpId = Guid.NewGuid();

        var fakePromotionServiceClient = Substitute.For<IPromotionServiceClient>();

        var sut = new PromotionCounter(
            promotionMainInfo,
            fakePromotionServiceClient,
            Array.Empty<string>());

        fakePromotionServiceClient
            .WhenCallGetPromotionRuleAsync(promotionMainInfo.Id)
            .ReturnRuleJson($$"""
                {
                    "campaignDetail": {
                        "percentage": 10,
                        "gameType": 15,
                        "gameProviderIds": ["{{targetGpId}}"],
                        "turnoverMultiple": 30,
                        "bonusAmountLimit": 5,
                        "defaultJoin": true,
                        "vipLevels": [1]
                    }
                }
                """);

        await sut.InitialAsync();

        var statistics = new[]
        {
            new GameRoundStatistics(
                "test1",
                1,
                promotionMainInfo.BrandId,
                targetGpId,
                GameType.Slot,
                100M,
                DateOnly.Parse("2022-11-29"))
        };

        var expectedBonus = 5M;
        var expectedBucketAmount = expectedBonus * 30M;

        // Act
        var actual = await sut.ProcessBatchAsync(statistics).ToArrayAsync();

        // Assert
        Assert.Single(actual);
        Assert.Equal(expectedBonus, actual.First().Bonus);
        Assert.Equal(expectedBucketAmount, actual.First().BucketAmount);
    }
}
