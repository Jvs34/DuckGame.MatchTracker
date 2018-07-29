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
		public String baseRecordingFolder;
		public String baseRepositoryUrl; //this has to be a github url for raw access

		//local paths
		public String roundsFolder;
		public String matchesFolder;
		public String timestampFormat;

		public String roundDataFile;
		public String roundVideoFile;
		public String roundVoiceFile;
		public String globalDataFile;

		public String DateTimeToString( DateTime time )
		{
			return time.ToString( timestampFormat );
		}

		public String GetRecordingFolder()
		{
			return baseRecordingFolder;
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

		public String GetRepositoryUrl()
		{
			return baseRepositoryUrl;
		}

		public String GetGlobalUrl()
		{
			return Url.Combine( GetRepositoryUrl() , globalDataFile );
		}

		public String GetMatchUrl( String matchName )
		{
			String matchFolder = Url.Combine( GetRepositoryUrl() , matchesFolder );
			String matchPath = Url.Combine( matchFolder , matchName + ".json" );
			return matchPath;
		}

		public String GetRoundUrl( String roundName )
		{
			String roundFolder = Url.Combine( GetRepositoryUrl() , roundsFolder );
			String roundFile = Url.Combine( roundFolder , roundName );
			return Url.Combine( roundFile , roundDataFile );
		}
	}
}