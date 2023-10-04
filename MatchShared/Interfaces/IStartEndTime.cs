using System;

namespace MatchTracker;

public interface IStartEndTime : IStartTime, IEndTime
{
	TimeSpan GetDuration();
}