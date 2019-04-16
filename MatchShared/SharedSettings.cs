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
		public string baseRecordingFolder { get; set; }

		public string baseRepositoryUrl { get; set; } //this has to be a github url for raw access

		public string globalDataFile { get; set; }

		public string matchesFolder { get; set; }

		public string roundDataFile { get; set; }

		//local paths
		public string roundsFolder { get; set; }

		public string roundVideoFile { get; set; }
		public string roundVoiceFile { get; set; }
		public string timestampFormat { get; set; }

		public string DateTimeToString( DateTime time )
		{
			return time.ToString( timestampFormat );
		}

		public string GetGlobalPath()
		{
			return Path.Combine( GetRecordingFolder() , globalDataFile );
		}

		public string GetGlobalUrl()
		{
			return Url.Combine( GetRepositoryUrl() , globalDataFile );
		}

		public string GetMatchPath( string matchName )
		{
			string matchFolder = Path.Combine( GetRecordingFolder() , matchesFolder );
			string matchPath = Path.Combine( matchFolder , matchName );
			return Path.ChangeExtension( matchPath , "json" );
		}

		public string GetMatchUrl( string matchName )
		{
			string matchFolder = Url.Combine( GetRepositoryUrl() , matchesFolder );
			string matchPath = Url.Combine( matchFolder , matchName + ".json" );
			return matchPath;
		}

		public string GetRecordingFolder()
		{
			return baseRecordingFolder;
		}

		public string GetRepositoryUrl()
		{
			return baseRepositoryUrl;
		}

		public string GetRoundPath( string roundName )
		{
			string roundFolder = Path.Combine( GetRecordingFolder() , roundsFolder );
			string roundFile = Path.Combine( roundFolder , roundName );
			return Path.Combine( roundFile , roundDataFile );
		}

		public string GetRoundUrl( string roundName )
		{
			string roundFolder = Url.Combine( GetRepositoryUrl() , roundsFolder );
			string roundFile = Url.Combine( roundFolder , roundName );
			return Url.Combine( roundFile , roundDataFile );
		}

		public string GetRoundVideoPath( string roundName )
		{
			string roundFolder = Path.Combine( GetRecordingFolder() , roundsFolder );
			string roundFile = Path.Combine( roundFolder , roundName );
			return Path.Combine( roundFile , roundVideoFile );
		}

		public string GetRoundVideoUrl( string roundName )
		{
			string roundFolder = Url.Combine( GetRepositoryUrl() , roundsFolder );
			string roundFile = Url.Combine( roundFolder , roundName );
			return Url.Combine( roundFile , roundVideoFile );
		}

		public string GetRoundVoicePath( string roundName )
		{
			string roundFolder = Path.Combine( GetRecordingFolder() , roundsFolder );
			string roundFile = Path.Combine( roundFolder , roundName );
			return Path.Combine( roundFile , roundVoiceFile );
		}

		public string GetRoundVoiceUrl( string roundName )
		{
			string roundFolder = Url.Combine( GetRepositoryUrl() , roundsFolder );
			string roundFile = Url.Combine( roundFolder , roundName );
			return Url.Combine( roundFile , roundVoiceFile );
		}
	}
}