using System;
using System.Linq;
#if !BLAZOR
using System.IO;
using Newtonsoft.Json;
#endif

namespace MatchTracker
{
	//a settings class that will be used for all match programs and website with blazor
	//this is something that once setup really shouldn't be touched again
	public class SharedSettings
	{
		//global paths
		public String baseRecordingFolder;
		public String debugBaseRecordingFolder; //still debating over this one

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
					winnerName = winners.First().GetName();
				}
				
			}

			return winnerName;
		}

#if !BLAZOR

		//these are supposed to be used for either the filesystem based Gets or the URL based ones
		public GlobalData DeserializeGlobalData( String data )
		{
			return JsonConvert.DeserializeObject<GlobalData>( data );
		}

		public MatchData DeserializeMatchData( String data )
		{
			return JsonConvert.DeserializeObject<MatchData>( data );
		}

		public RoundData DeserializeRoundData( String data )
		{
			return JsonConvert.DeserializeObject<RoundData>( data );
		}

		//utility functions for saving, these will obviously not work on the blazor website so we're not going to include them
		public GlobalData GetGlobalData()
		{
			String globalDataPath = GetGlobalPath();
			return DeserializeGlobalData( File.ReadAllText( globalDataPath ) );
		}

		public MatchData GetMatchData( String matchName )
		{
			String matchPath = GetMatchPath( matchName );
			return DeserializeMatchData( File.ReadAllText( matchPath ) );
		}

		public RoundData GetRoundData( String roundName )
		{
			String roundPath = GetRoundPath( roundName );
			return DeserializeRoundData( File.ReadAllText( roundPath ) );
		}

		public String GetGlobalPath()
		{
			return Path.Combine( GetRecordingFolder() , globalDataFile );
		}

		public String GetMatchPath( String matchName )
		{
			String matchFolder = Path.Combine( GetRecordingFolder() , matchesFolder );
			String matchPath = Path.Combine( matchFolder , matchName );
			return Path.ChangeExtension( matchPath , "json" );
		}

		public String GetRoundPath( String roundName )
		{
			String roundFolder = Path.Combine( GetRecordingFolder() , roundsFolder );
			String roundFile = Path.Combine( roundFolder , roundName );
			return Path.Combine( roundFile , roundDataFile );
		}

		public void SaveGlobalData( GlobalData globalData )
		{
			File.WriteAllText( GetGlobalPath() , JsonConvert.SerializeObject( globalData , Formatting.Indented ) );
		}

		public void SaveMatchData( String matchName , MatchData matchData )
		{
			File.WriteAllText( GetMatchPath( matchName ) , JsonConvert.SerializeObject( matchData , Formatting.Indented ) );
		}

		public void SaveRoundData( String roundName , RoundData roundData )
		{
			File.WriteAllText( GetRoundPath( roundName ) , JsonConvert.SerializeObject( roundData , Formatting.Indented ) );
		}

#endif

	}
}