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
    [HttpPost("settle")]
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
}
