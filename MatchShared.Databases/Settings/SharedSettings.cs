using Flurl;
using System;
using System.IO;

namespace MatchTracker
{
	//a settings class that will be used for all match programs and website with blazor
	//this is something that once setup really shouldn't be touched again
	public class SharedSettings
	{
		public string BaseRecordingFolder { get; set; }
		public string BaseRepositoryUrl { get; set; } //this has to be a github url for raw access
		public string DataName { get; set; }
		public string RoundVideoFile { get; set; }
		public string RoundVoiceFile { get; set; }
		public string TimestampFormat { get; set; }
		public string RoundReplayFile { get; set; }
		public string RoundReplayFileCompressed { get; set; }
		public string RepositoryUser { get; set; }
		public string RepositoryName { get; set; }
		public string DatabaseFile { get; set; }
		public string LevelPreviewFile { get; set; }

		public string DateTimeToString( DateTime time ) => time.ToString( TimestampFormat );
		public static string Combine( bool isUrl , params string [] paths ) => isUrl ? Url.Combine( paths ) : Path.Combine( paths );
		public string GetRecordingFolder( bool useUrl = false ) => useUrl ? BaseRepositoryUrl : BaseRecordingFolder;
		public string GetPath<T>( string databaseIndex , bool useUrl = false ) where T : IDatabaseEntry => GetPath( typeof( T ).Name , databaseIndex , useUrl );
		public string GetPath( string typeName , string databaseIndex , bool useUrl = false ) => Combine( useUrl , GetRecordingFolder( useUrl ) , typeName , string.IsNullOrEmpty( databaseIndex ) ? typeName : databaseIndex );
		public string GetDataPath<T>( string databaseIndex = "" , bool useUrl = false ) where T : IDatabaseEntry => Combine( useUrl , GetPath<T>( databaseIndex , useUrl ) , DataName );
		public string GetDataPath( string typeName , string databaseIndex = "" , bool useUrl = false ) => Combine( useUrl , GetPath( typeName , databaseIndex , useUrl ) , DataName );
		public string GetDatabasePath( bool useUrl = false ) => Combine( useUrl , GetRecordingFolder( useUrl ) , DatabaseFile );
		public string GetRoundVideoPath( string roundName , bool useUrl = false ) => Combine( useUrl , GetPath<RoundData>( roundName , useUrl ) , RoundVideoFile );
		public string GetMatchVideoPath( string matchName , bool useUrl = false ) => Combine( useUrl , GetPath<MatchData>( matchName , useUrl ) , RoundVideoFile );
		public string GetLevelPreviewPath( string levelName , bool useUrl = false ) => Combine( useUrl , GetPath<LevelData>( levelName , useUrl ) , LevelPreviewFile );
	}
}