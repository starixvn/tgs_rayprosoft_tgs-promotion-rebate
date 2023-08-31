using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;

using Tgs.MessageQueue;
using Tgs.Promotion.RebateService.Protos;
using Tgs.WebApi.Toolkits;

namespace Tgs.Promotion.RebateService.Controllers;

[Route("[controller]")]
[ApiController]
public class ScheduleController : ControllerBase
{
	[HttpPost("settle", Name = "ExecuteDailySettlement")]
	[SuccessStatusCode(202)]
	public async ValueTask ExecuteDailySettlementAsync(
		[FromServices] IMessageSender messageSender)
	{
		var utcNow = DateTime.UtcNow;

		var request = new DailySettlementRequest
		{
			SettleDateTime = Timestamp.FromDateTime(new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, DateTimeKind.Utc))
		};

		await messageSender.PublishAsync(
			"tgs.rebate.ipc.daily-settle",
			request.ToByteArray(),
			HttpContext.RequestAborted).ConfigureAwait(false);
	}

	[HttpPost("settle/{reportDate}/{utcHour}", Name = "ExecuteDailySettlementByDate")]
	[SuccessStatusCode(202)]
	public async ValueTask ExecuteDailySettlementAsync(
		[FromServices] IMessageSender messageSender,
		DateOnly reportDate,
		int utcHour)
	{
		var reportDateTime = new DateTimeOffset(reportDate.Year, reportDate.Month, reportDate.Day,utcHour,0,0, TimeSpan.Zero);

		var request = new DailySettlementRequest
		{
			SettleDateTime = Timestamp.FromDateTime(reportDateTime.UtcDateTime)
		};

		await messageSender.PublishAsync(
			"tgs.rebate.ipc.daily-settle",
			request.ToByteArray(),
			HttpContext.RequestAborted).ConfigureAwait(false);
	}

	[HttpPost("bonus", Name = "ExecuteDistribute")]
	[SuccessStatusCode(202)]
	public async Task ExecuteDistributeAsync(
		[FromServices] IMessageSender messageSender)
	{
		var utcNow = DateTime.UtcNow;

		var request = new RebateDistributeRequest
		{
			DistributeDateTime = Timestamp.FromDateTime(new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, 0, 0, DateTimeKind.Utc))
		};

		await messageSender.PublishAsync(
			"tgs.rebate.ipc.daily-bonus",
			request.ToByteArray(),
			HttpContext.RequestAborted).ConfigureAwait(false);
	}

	[HttpPost("bonus/{distributeDate}/{utcHour}", Name = "ExecuteDistributeByDate")]
	[SuccessStatusCode(202)]
	public async Task ExecuteDistributeAsync(
		[FromServices] IMessageSender messageSender,
		DateOnly distributeDate,
		int utcHour)
	{
		var distributeDateTime = new DateTimeOffset(distributeDate.Year, distributeDate.Month, distributeDate.Day, utcHour, 0, 0, TimeSpan.Zero);

		var request = new RebateDistributeRequest
		{
			DistributeDateTime = Timestamp.FromDateTime(distributeDateTime.UtcDateTime)
		};

		await messageSender.PublishAsync(
			"tgs.rebate.ipc.daily-bonus",
			request.ToByteArray(),
			HttpContext.RequestAborted).ConfigureAwait(false);
	}

	[HttpPost("supplementary/{reportDate}/{utcHour}", Name = "SupplementaryDailySettlement")]
	[SuccessStatusCode(202)]
	public async Task SupplementaryDailySettlementAsync(
		[FromServices] IMessageSender messageSender,
		DateOnly reportDate,
		int utcHour)
	{
		var reportDateTime = new DateTimeOffset(reportDate.Year, reportDate.Month, reportDate.Day, utcHour, 0, 0, TimeSpan.Zero);

		var request = new DailySettlementRequest
		{
			SettleDateTime = Timestamp.FromDateTime(reportDateTime.UtcDateTime)
		};

		await messageSender.PublishAsync(
			"tgs.rebate.ipc.supplementary-daily-settle",
			request.ToByteArray(),
			HttpContext.RequestAborted).ConfigureAwait(false);
	}
}
