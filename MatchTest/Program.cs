using MatchTracker;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MatchTest
{
	internal static class Program
	{
		private static async Task Main( string [] args )
		{
			var fuckshit = "2018-04-28 20-19-04";

			IGameDatabase db = new FileSystemGameDatabase
			{
				SharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( Path.Combine( "Settings" , "shared.json" ) ) )
			};
			await db.Load();


			fuckshit = ( await db.GetAll<RoundData>() ).Last();


			var jsondb = new FlatJsonNetGameDatabase()
			//var jsondb = new FlatJsonGameDatabase()
			{
				SharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( Path.Combine( "Settings" , "shared.json" ) ) )
			};

			jsondb.SharedSettings.BaseRecordingFolder = @"C:\Users\Jvsth.000.000\Desktop\";
			jsondb.SharedSettings.DatabaseFile = "data.json";

			await jsondb.Load();

			//await jsondb.SaveData( await db.GetData<RoundData>( fuckshit ) );

			var roundData = await jsondb.GetData<RoundData>( fuckshit );
			int i = 5;
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
	}
}