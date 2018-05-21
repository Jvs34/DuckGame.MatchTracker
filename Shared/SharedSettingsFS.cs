using System;
using System.IO;
using Newtonsoft.Json;

namespace MatchTracker
{
	public partial class SharedSettings
	{
		//these are supposed to be used for the filesystem based Gets, which is why they're in a separate file so we just don't include them in the blazor project
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
	}
}