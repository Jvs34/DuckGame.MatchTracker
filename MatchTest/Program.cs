using MatchTracker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MatchTest
{
	internal static class Program
	{
		private static async Task Main( string [] args )
		{
			var fuckshit = "2018-04-28 20-19-04";

			


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