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
		public string DatabaseFile { get; set; }
		public string LevelPreviewFile { get; set; }

		public string DateTimeToString( DateTime time ) => time.ToString( TimestampFormat );
		public static string Combine( params string [] paths ) => Path.Combine( paths );
		public string GetRecordingFolder() => BaseRecordingFolder;
		public string GetPath<T>( string databaseIndex ) where T : IDatabaseEntry => GetPath( typeof( T ).Name , databaseIndex );
		public string GetPath( string typeName , string databaseIndex ) => Combine( GetRecordingFolder() , typeName , string.IsNullOrEmpty( databaseIndex ) ? typeName : databaseIndex );
		public string GetDataPath<T>( string databaseIndex = "" ) where T : IDatabaseEntry => Combine( GetPath<T>( databaseIndex ) , DataName );
		public string GetDataPath( string typeName , string databaseIndex = "" ) => Combine( GetPath( typeName , databaseIndex ) , DataName );
		public string GetDatabasePath() => Combine( GetRecordingFolder() , DatabaseFile );
		public string GetRoundVideoPath( string roundName ) => Combine( GetPath<RoundData>( roundName ) , RoundVideoFile );
		public string GetMatchVideoPath( string matchName ) => Combine( GetPath<MatchData>( matchName ) , RoundVideoFile );
		public string GetLevelPreviewPath( string levelName ) => Combine( GetPath<LevelData>( levelName ) , LevelPreviewFile );
	}
}