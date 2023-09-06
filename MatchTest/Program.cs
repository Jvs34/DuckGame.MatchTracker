using MatchTracker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MatchTest
{
	internal static class Program
	{
		private static async Task Main( string [] args )
		{

			using IGameDatabase litedb = new LiteDBGameDatabase()
			{
				SharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( Path.Combine( "Settings" , "shared.json" ) ) )
			};
			await litedb.Load();

			var data = await litedb.GetData<RoundData>( "2018-09-24 22-53-23" );

			Console.ReadKey();
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
		}

		private static async Task DeleteAllJsonFiles( FileSystemGameDatabase fileSystemGameDatabase )
		{
			var mainpath = fileSystemGameDatabase.SharedSettings.BaseRecordingFolder;
			var filestodelete = Directory.EnumerateFiles( mainpath , "*.json" , new EnumerationOptions()
			{
				RecurseSubdirectories = true ,
				ReturnSpecialDirectories = false
			} ).ToList();

			filestodelete.ForEach( x => File.Delete( x ) );
		}

		private static async Task CopyDatabaseOverTo( IGameDatabase from , IGameDatabase to )
		{
			var backup = await from.GetBackupAllOut();
			await to.ImportBackup( backup );
		}
	}
}