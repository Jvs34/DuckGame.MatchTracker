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
		public string DatabaseFile { get; set; }
		public string LevelPreviewFile { get; set; }

		public string DateTimeToString( DateTime time ) => time.ToString( TimestampFormat );

		public static string Combine( Func<string [] , string> combineFunc = null , params string [] paths )
		{
			static string DefaultCombineFunction( string [] paths ) => Path.Combine( paths );
			combineFunc ??= DefaultCombineFunction;
			return combineFunc( paths );
		}

		public string GetRecordingFolder() => BaseRecordingFolder;
		public string GetPath<T>( string databaseIndex ) where T : IDatabaseEntry => GetPath( typeof( T ).Name , databaseIndex );
		public string GetPath( string typeName , string databaseIndex ) => Combine( null , GetRecordingFolder() , typeName , string.IsNullOrEmpty( databaseIndex ) ? typeName : databaseIndex );
		public string GetDataPath<T>( string databaseIndex = "" ) where T : IDatabaseEntry => Combine( null , GetPath<T>( databaseIndex ) , DataName );
		public string GetDataPath( string typeName , string databaseIndex = "" ) => Combine( null , GetPath( typeName , databaseIndex ) , DataName );
		public string GetDatabasePath() => Combine( null , GetRecordingFolder() , DatabaseFile );
		public string GetRoundVideoPath( string roundName ) => Combine( null , GetPath<RoundData>( roundName ) , RoundVideoFile );
		public string GetMatchVideoPath( string matchName ) => Combine( null , GetPath<MatchData>( matchName ) , RoundVideoFile );
		public string GetLevelPreviewPath( string levelName ) => Combine( null , GetPath<LevelData>( levelName ) , LevelPreviewFile );
	}
}