using Google.Protobuf;

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
		var currentTime = DateTimeOffset.UtcNow;
		var currentReportDate = DateOnly.FromDateTime(
			currentTime.ToOffset(TimeSpan.FromHours(8D)).DateTime);

		var request = new DailySettlementRequest
		{
			ReportDate = currentReportDate
		};

		await messageSender.PublishAsync(
			"tgs.rebate.ipc.daily-settle",
			request.ToByteArray(),
			HttpContext.RequestAborted).ConfigureAwait(false);
	}

	[HttpPost("settle/{reportDate}", Name = "ExecuteDailySettlementByDate")]
	[SuccessStatusCode(202)]
	public async ValueTask ExecuteDailySettlementAsync(
		[FromServices] IMessageSender messageSender,
		DateOnly reportDate)
	{
		var request = new DailySettlementRequest
		{
			ReportDate = reportDate
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
		var currentTime = DateTimeOffset.UtcNow;
		var currentReportDate = DateOnly.FromDateTime(
			currentTime.ToOffset(TimeSpan.FromHours(8D)).DateTime);

		var request = new RebateDistributeRequest
		{
			DistributeDate = currentReportDate
		};

		await messageSender.PublishAsync(
			"tgs.rebate.ipc.daily-bonus",
			request.ToByteArray(),
			HttpContext.RequestAborted).ConfigureAwait(false);
	}

	[HttpPost("bonus/{distributeDate}", Name = "ExecuteDistributeByDate")]
	[SuccessStatusCode(202)]
	public async Task ExecuteDistributeAsync(
		[FromServices] IMessageSender messageSender,
		DateOnly distributeDate)
	{
		var request = new RebateDistributeRequest
		{
			DistributeDate = distributeDate
		};

		await messageSender.PublishAsync(
			"tgs.rebate.ipc.daily-bonus",
			request.ToByteArray(),
			HttpContext.RequestAborted).ConfigureAwait(false);
	}
}
