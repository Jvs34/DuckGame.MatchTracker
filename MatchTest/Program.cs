using MatchShared.Databases;
using MatchShared.Databases.LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

var configuration = new ConfigurationBuilder()
	.SetBasePath( Directory.GetCurrentDirectory() )
	.AddJsonFile( Path.Combine( "Settings", "shared.json" ) )
.Build();

using var db = new LiteDBGameDatabase();

configuration.Bind( db.SharedSettings );
await db.Load();


//await db.IterateOverAll<PlayerData>( async playerData =>
//{
//	await db.SaveData( playerData );
//	return true;
//} );

Console.WriteLine( "Done, press a key to stop" );
Console.ReadKey();


#region bullshit
//var emptyMatchNames = new ConcurrentBag<string>();

//await db.IterateOverAll<MatchData>( async data =>
//{
//	return await ClearTags( data , db );
//} );

//await db.IterateOverAll<RoundData>( async data =>
//{
//	return await ClearTags( data , db );
//} );

//await db.IterateOverAll<LevelData>( async data =>
//{
//	return await ClearTags( data , db );
//} );



//var entries = await db.GetAllIndexes<RoundData>();
//using var roundDataStream = File.OpenWrite( Path.Combine( @"C:\Users\Jvsth\Desktop\waw" , "rounddata.json" ) );

//using var writer = new Utf8JsonWriter( roundDataStream , new JsonWriterOptions()
//{
//	Indented = true ,
//} );

//writer.WriteStartObject();

//foreach( var roundName in entries )
//{
//	var roundData = await db.GetData<RoundData>( roundName );
//	if( roundData is not null )
//	{
//		writer.WritePropertyName( roundName );
//		JsonSerializer.Serialize( writer , roundData );
//	}
//}

//writer.WriteEndObject();
//var litedb = db.Database;

//var path = @"RoundData.json";

//litedb.Execute( $"select $ into $file('{path}') from RoundData" );
//var allData = await db.GetBackup<RoundData>();

//static async Task<bool> ClearTags<T>( T data , LiteDBGameDatabase db ) where T : ITagsList, IDatabaseEntry
//{
//	data.Tags.Clear();
//	await db.SaveData<T>( data );
//	return true;
//}



//var data = await litedb.GetData<RoundData>( "2018-09-24 22-53-23" );


//{

//	var allData = await db.GetBackupGeneric<RoundData>();

//	var transformed = allData.Select( x => x.Value );

//	await using var jsonFileTest = File.OpenWrite( Path.Combine( Path.GetTempPath() , "test.json" ) );
//	await JsonSerializer.SerializeAsync( jsonFileTest , transformed , new JsonSerializerOptions()
//	{
//		WriteIndented = true ,
//	} );
//}

//{
//	await using var jsonFileTest = File.OpenRead( Path.Combine( Path.GetTempPath() , "test.json" ) );

//	await foreach( var roundData in JsonSerializer.DeserializeAsyncEnumerable<RoundData>( jsonFileTest ) )
//	{
//		Console.WriteLine( roundData.DatabaseIndex );
//	}

//	//using var document = await JsonDocument.ParseAsync( jsonFileTest , new JsonDocumentOptions()
//	//{
//	//	AllowTrailingCommas = true
//	//} );

//	Console.WriteLine( "haha document" );
//}


//convert IVideoUpload to IVideoUploadList

//Console.WriteLine( "Resaving matches" );
//await db.IterateOverAll<MatchData>( async ( entry ) =>
//{
//	//foreach( var upload in entry.VideoUploads )
//	//{
//	//	upload.RecordingType = RecordingType.Video;
//	//}

//	await db.SaveData( entry );
//	return true;
//} );

//Console.WriteLine( "Resaving rounds" );
//await db.IterateOverAll<RoundData>( async ( entry ) =>
//{
//	//foreach( var upload in entry.VideoUploads )
//	//{
//	//	upload.RecordingType = RecordingType.Video;
//	//}

//	await db.SaveData( entry );
//	return true;
//} );


//JsonDocument jsonDocument = JsonDocument.Parse







//static async Task MigrateIVideoUploadToIVideoUploadList<T>( IGameDatabase db , T databaseEntry ) where T : IDatabaseEntry, IVideoUpload, IVideoUploadList
//{
//	databaseEntry.VideoUploads ??= new List<VideoUpload>();

//	if( databaseEntry.VideoUploads.Count > 0 )
//	{
//		return;
//	}

//	var videoUpload = new VideoUpload()
//	{
//		//when there's entries without a videotype, that means that they were made with NoVideoRecorder
//		ServiceType = databaseEntry.VideoType == VideoUrlType.None ? VideoServiceType.None : VideoServiceType.Youtube ,
//		Url = databaseEntry.YoutubeUrl ,
//		VideoType = databaseEntry.VideoType
//	};

//	databaseEntry.VideoUploads.Add( videoUpload );

//	await db.SaveData( databaseEntry );
//}

//await CopyDatabaseOverTo( filedb , litedb );
//await DeleteAllJsonFiles( filedb as FileSystemGameDatabase );
/*
var fuckshit = "2018-04-28 20-19-04";
fuckshit = ( await db.GetAll<RoundData>() ).Last();


var jsondb = new FlatJsonNetGameDatabase()
//var jsondb = new FlatJsonGameDatabase()
{
	SharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( Path.Combine( "Settings" , "shared.json" ) ) )
};

/*
jsondb.SharedSettings.BaseRecordingFolder = @"C:\Users\Jvsth.000.000\Desktop\";
jsondb.SharedSettings.DatabaseFile = "data.json";

await jsondb.Load();

//await jsondb.SaveData( await db.GetData<RoundData>( fuckshit ) );

var roundData = await jsondb.GetData<RoundData>( fuckshit );
/*
Console.WriteLine( "Backing up..." );
var backup = await db.GetBackup();
Console.WriteLine( "Importing the backup..." );
await jsondb.ImportBackup( backup );
*/

/*
IGameDatabase db = new FileSystemGameDatabase
{
	SharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( Path.Combine( "Settings" , "shared.json" ) ) )
};


await db.Load();




using JsonDataStoreGameDatabase jsonDB = new JsonDataStoreGameDatabase()
{
	SharedSettings = db.SharedSettings
};

await jsonDB.Load();

await db.IterateOverAll<PlayerData>( async ( data ) =>
{
	await jsonDB.SaveData( data );

	return true;
} );
*/

/*var backup = await db.GetBackup();
foreach( var backupKV in backup )
{
	foreach( var dataBackupKV in backupKV.Value )
	{
		await jsonDB.SaveData( dataBackupKV.Value );
	}
}
*/

//static async Task CopyDatabaseOverTo( IGameDatabase from , IGameDatabase to )
//{
//	var backup = await from.GetBackupAllOut();
//	await to.ImportBackup( backup );
//}
#endregion bullshit