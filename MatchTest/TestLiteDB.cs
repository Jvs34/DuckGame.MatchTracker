/*
using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MatchTest
{
	public class TestLiteDB
	{
		public async Task Test()
		{
			var Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "firebase.json" )
			.Build();

			IGameDatabase defaultDatabase = new FileSystemGameDatabase();

			HttpClient httpClient = new HttpClient();

			IGameDatabase liteDb = new FirebaseGameDatabase(
				httpClient
			);

			//IGameDatabase liteDb = new LiteDBGameDatabase();


			Configuration.Bind( defaultDatabase.SharedSettings );
			Configuration.Bind( liteDb.SharedSettings );

			if( liteDb is FirebaseGameDatabase firedb )
			{
				Configuration.Bind( firedb.FirebaseSettings );
			}

			await liteDb.Load();

			await defaultDatabase.Load();

			using( var fileWriter = File.CreateText( @"C:\Users\Jvsth.000.000\Desktop\duckgayimport.json" ) )
			{

				JsonSerializer serializer = new JsonSerializer()
				{
					Formatting = Formatting.Indented ,
				};

				var mainCollection = await defaultDatabase.GetBackup();
				serializer.Serialize( fileWriter , mainCollection );
			}

		}
	}
}

*/