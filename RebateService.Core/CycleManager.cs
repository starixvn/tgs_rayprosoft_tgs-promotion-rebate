using NCrontab;

namespace Tgs.Services.Rebate;

internal class CycleManager
{
	private readonly DateTime _startTime;
	private readonly DateTime _endTime;
	private readonly CrontabSchedule _crontab;

	public CycleManager(string cronExpression, DateTime startTime, DateTime endTime)
	{
		_crontab = CrontabSchedule.Parse(
			cronExpression,
			new CrontabSchedule.ParseOptions
			{
				IncludingSeconds = false
			});
		_startTime = startTime;
		_endTime = endTime;
	}

	public DateTime? GetNextOccurrence(DateTime input)
		=> _crontab.GetNextOccurrences(input.AddSeconds(-1D), _endTime).Cast<DateTime?>().FirstOrDefault();

	public DateTime? GetPreviousOccurrence(DateTime input)
		=> _crontab.GetNextOccurrences(_startTime, input).Cast<DateTime?>().LastOrDefault();
}
