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
		public string LevelsPreviewFolder { get; set; }

		public string DatabaseFile { get; set; }

		public string DateTimeToString( DateTime time ) => time.ToString( TimestampFormat );

		private string Combine( bool isUrl , params string [] paths ) => isUrl ? Url.Combine( paths ) : Path.Combine( paths );

		public string GetDatabasePath( bool useUrl = false ) => Combine( useUrl , GetRecordingFolder( useUrl ) , DatabaseFile );

		public string GetGlobalPath( bool useUrl = false ) => Combine( useUrl , GetRecordingFolder( useUrl ) , GlobalDataFile );

		public string GetMatchPath( string matchName , bool useUrl = false )
		{
			string matchFolder = Combine( useUrl , GetRecordingFolder( useUrl ) , MatchesFolder );
			return Combine( useUrl , matchFolder , matchName + ".json" );
		}

		public string GetRecordingFolder( bool useUrl = false ) => useUrl ? BaseRepositoryUrl : BaseRecordingFolder;

		public string GetRoundPath( string roundName , bool useUrl = false )
		{
			string roundFolder = Combine( useUrl , GetRecordingFolder( useUrl ) , RoundsFolder );
			string roundFile = Combine( useUrl , roundFolder , roundName );
			return Combine( useUrl , roundFile , RoundDataFile );
		}


		public string GetRoundVideoPath( string roundName , bool useUrl = false )
		{
			string roundFolder = Combine( useUrl , GetRecordingFolder( useUrl ) , RoundsFolder );
			string roundFile = Combine( useUrl , roundFolder , roundName );
			return Combine( useUrl , roundFile , RoundVideoFile );
		}

		public string GetRoundVoicePath( string roundName , bool useUrl = false )
		{
			string roundFolder = Combine( useUrl , GetRecordingFolder( useUrl ) , RoundsFolder );
			string roundFile = Combine( useUrl , roundFolder , roundName );
			return Combine( useUrl , roundFile , RoundVoiceFile );
		}
	}
}