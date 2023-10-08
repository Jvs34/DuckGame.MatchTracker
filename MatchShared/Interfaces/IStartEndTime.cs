using System;

namespace MatchShared.Interfaces;

public interface IStartEndTime : IStartTime, IEndTime
{
	TimeSpan GetDuration();
}