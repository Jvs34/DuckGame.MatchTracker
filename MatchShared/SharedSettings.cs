using Flurl;
using System;
using System.IO;

namespace MatchTracker
{
	//a settings class that will be used for all match programs and website with blazor
	//this is something that once setup really shouldn't be touched again
	public class SharedSettings
	{
		//global paths
		public string BaseRecordingFolder { get; set; }

		public string BaseRepositoryUrl { get; set; } //this has to be a github url for raw access

		public string GlobalDataFile { get; set; }

		public string MatchesFolder { get; set; }

		public string RoundDataFile { get; set; }

		//local paths
		public string RoundsFolder { get; set; }

		public string RoundVideoFile { get; set; }
		public string RoundVoiceFile { get; set; }
		public string TimestampFormat { get; set; }

		public string DateTimeToString( DateTime time )
		{
			return time.ToString( TimestampFormat );
		}

		public string GetGlobalPath()
		{
			return Path.Combine( GetRecordingFolder() , GlobalDataFile );
		}

		public string GetGlobalUrl()
		{
			return Url.Combine( GetRepositoryUrl() , GlobalDataFile );
		}

		public string GetMatchPath( string matchName )
		{
			string matchFolder = Path.Combine( GetRecordingFolder() , MatchesFolder );
			string matchPath = Path.Combine( matchFolder , matchName );
			return Path.ChangeExtension( matchPath , "json" );
		}

		public string GetMatchUrl( string matchName )
		{
			string matchFolder = Url.Combine( GetRepositoryUrl() , MatchesFolder );
			string matchPath = Url.Combine( matchFolder , matchName + ".json" );
			return matchPath;
		}

		public string GetRecordingFolder()
		{
			return BaseRecordingFolder;
		}

		public string GetRepositoryUrl()
		{
			return BaseRepositoryUrl;
		}

		public string GetRoundPath( string roundName )
		{
			string roundFolder = Path.Combine( GetRecordingFolder() , RoundsFolder );
			string roundFile = Path.Combine( roundFolder , roundName );
			return Path.Combine( roundFile , RoundDataFile );
		}

		public string GetRoundUrl( string roundName )
		{
			string roundFolder = Url.Combine( GetRepositoryUrl() , RoundsFolder );
			string roundFile = Url.Combine( roundFolder , roundName );
			return Url.Combine( roundFile , RoundDataFile );
		}

		public string GetRoundVideoPath( string roundName )
		{
			string roundFolder = Path.Combine( GetRecordingFolder() , RoundsFolder );
			string roundFile = Path.Combine( roundFolder , roundName );
			return Path.Combine( roundFile , RoundVideoFile );
		}

		public string GetRoundVideoUrl( string roundName )
		{
			string roundFolder = Url.Combine( GetRepositoryUrl() , RoundsFolder );
			string roundFile = Url.Combine( roundFolder , roundName );
			return Url.Combine( roundFile , RoundVideoFile );
		}

		public string GetRoundVoicePath( string roundName )
		{
			string roundFolder = Path.Combine( GetRecordingFolder() , RoundsFolder );
			string roundFile = Path.Combine( roundFolder , roundName );
			return Path.Combine( roundFile , RoundVoiceFile );
		}

		public string GetRoundVoiceUrl( string roundName )
		{
			string roundFolder = Url.Combine( GetRepositoryUrl() , RoundsFolder );
			string roundFile = Url.Combine( roundFolder , roundName );
			return Url.Combine( roundFile , RoundVoiceFile );
		}
	}
}