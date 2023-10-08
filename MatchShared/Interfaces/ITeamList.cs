using MatchShared.DataClasses;
using System.Collections.Generic;

namespace MatchShared.Interfaces;

public interface ITeamsList
{
	List<TeamData> Teams { get; set; }
}