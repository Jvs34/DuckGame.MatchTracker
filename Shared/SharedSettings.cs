using System;

namespace MatchTracker
{
	//a settings class that will be used for all match programs and website with blazor
	//this is something that once setup really shouldn't be touched again
	public partial class SharedSettings
	{
		//global paths
		public String baseRecordingFolder;
		public String debugBaseRecordingFolder; //still debating over this one
		public String baseRepositoryUrl; //this has to be a github url for raw access

		//local paths
		public String roundsFolder = "rounds";
		public String matchesFolder = "matches";
		public String timestampFormat = "yyyy-MM-dd HH-mm-ss";

		public String roundDataFile = "rounddata.json";
		public String roundVideoFile = "video.mp4";
		public String globalDataFile = "data.json";

		public String DateTimeToString( DateTime time )
		{
			return time.ToString( timestampFormat );
		}

		public String GetRecordingFolder()
		{
#if DEBUG
			return debugBaseRecordingFolder;
#else
			return baseRecordingFolder;
#endif
		}

		public String GetRoundWinnerName( RoundData roundData )
		{
			String winnerName = "";

			//check if anyone actually won
			if( roundData.winner != null )
			{
				var winners = roundData.players.FindAll( p => p.team.hatName == roundData.winner.hatName );
				if( winners.Count > 1 )
				{
					winnerName = roundData.winner.hatName;
				}
				else
				{
					winnerName = winners[0].GetName();
				}

			}

			return winnerName;
		}

	}
}