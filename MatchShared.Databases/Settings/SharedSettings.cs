using MatchShared.DataClasses;
using MatchShared.Interfaces;
using System;
using System.IO;

namespace MatchShared.Databases.Settings;

/// <summary>
/// a settings class that will be used for all match programs and website with blazor,
/// this is something that once setup really shouldn't be touched again
/// </summary>
public sealed class SharedSettings
{
	public string BaseRecordingFolder { get; set; }
	public string DataName { get; set; }
	public string RoundVideoFile { get; set; }
	public string RoundVoiceFile { get; set; }
	public string TimestampFormat { get; set; }
	public string DatabaseFile { get; set; }
	public string LevelPreviewFile { get; set; }

	public string DateTimeToString( DateTime time ) => time.ToString( TimestampFormat );

	//TODO: now that I think about it, these have no business being in SharedSettings
	private static string Combine( params string[] paths ) => Path.Combine( paths );
	public string GetRecordingFolder() => BaseRecordingFolder;
	public string GetPath<T>( string databaseIndex ) where T : IDatabaseEntry => GetPath( typeof( T ).Name, databaseIndex );
	public string GetPath( string typeName, string databaseIndex ) => Combine( GetRecordingFolder(), typeName, string.IsNullOrEmpty( databaseIndex ) ? typeName : databaseIndex );
	public string GetDataPath<T>( string databaseIndex = "" ) where T : IDatabaseEntry => Combine( GetPath<T>( databaseIndex ), DataName );
	public string GetDataPath( string typeName, string databaseIndex = "" ) => Combine( GetPath( typeName, databaseIndex ), DataName );
	public string GetDatabasePath() => Combine( GetRecordingFolder(), DatabaseFile );
	public string GetRoundVideoPath( string roundName ) => Combine( GetPath<RoundData>( roundName ), RoundVideoFile );
	public string GetMatchVideoPath( string matchName ) => Combine( GetPath<MatchData>( matchName ), RoundVideoFile );
	public string GetLevelPreviewPath( string levelName ) => Combine( GetPath<LevelData>( levelName ), LevelPreviewFile );
}