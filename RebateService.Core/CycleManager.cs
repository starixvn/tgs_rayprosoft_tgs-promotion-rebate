using NCrontab;

namespace Tgs.Services.Rebate;

internal class CycleManager
{
	private readonly CrontabSchedule _crontab;
	private readonly DateTime _endTime;
	private readonly DateTime _startTime;

	public CycleManager(string cronExpression, DateTime startTime, DateTime endTime)
	{
		_crontab = CrontabSchedule.Parse(
			cronExpression,
			new() { IncludingSeconds = false });
		_startTime = startTime;
		_endTime = endTime;
	}

	public DateTime? GetNextOccurrence(DateTime input)
		=> _crontab.GetNextOccurrences(input.AddSeconds(-1D), _endTime).Cast<DateTime?>().FirstOrDefault();

	public DateTime? GetPreviousOccurrence(DateTime input)
		=> _crontab.GetNextOccurrences(_startTime, input).Cast<DateTime?>().LastOrDefault();
}