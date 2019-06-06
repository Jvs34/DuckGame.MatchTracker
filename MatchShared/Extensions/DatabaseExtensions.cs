using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MatchTracker
{
	public static class DatabaseExtensions
	{
		public static async Task<Dictionary<string , Dictionary<string , IDatabaseEntry>>> GetBackup( this IGameDatabase db )
		{
			var mainCollection = new Dictionary<string , Dictionary<string , IDatabaseEntry>>()
			{
				[nameof( GlobalData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( MatchData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( RoundData )] = new Dictionary<string , IDatabaseEntry>() ,
			};

			var globalData = await db.GetData<GlobalData>();

			mainCollection [nameof( GlobalData )] [globalData.DatabaseIndex] = globalData;

			foreach( var roundName in globalData.Rounds )
			{
				RoundData roundData = await db.GetData<RoundData>( roundName );

				mainCollection [nameof( RoundData )] [roundData.DatabaseIndex] = roundData;
			}

			foreach( var matchName in globalData.Matches )
			{
				MatchData matchData = await db.GetData<MatchData>( matchName );
				mainCollection [nameof( MatchData )] [matchData.DatabaseIndex] = matchData;
			}

			return mainCollection;
		}
	}
}
