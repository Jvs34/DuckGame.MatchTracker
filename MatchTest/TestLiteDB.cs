using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchTest
{
	public class TestLiteDB
	{
		public async Task Test()
		{
			IGameDatabase defaultDatabase = new FileSystemGameDatabase();

			IGameDatabase liteDb = new LiteDBGameDatabase();

			var Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
			.Build();

			Configuration.Bind( defaultDatabase.SharedSettings );
			Configuration.Bind( liteDb.SharedSettings );


			await defaultDatabase.Load();
			await liteDb.Load();



			GlobalData globalData = await defaultDatabase.GetData<GlobalData>();

			await liteDb.SaveData( globalData );

			foreach( var ply in globalData.Players )
			{
				await liteDb.SaveData( ply );
			}

			foreach( var lvl in globalData.Levels )
			{
				await liteDb.SaveData( lvl );
			}

			foreach( var tag in globalData.Tags )
			{
				await liteDb.SaveData( tag );
			}


			foreach( var roundName in globalData.Rounds )
			{
				RoundData roundData = await defaultDatabase.GetData<RoundData>( roundName );
				await liteDb.SaveData( roundData );
			}

			foreach( var matchName in globalData.Matches )
			{
				MatchData matchData = await defaultDatabase.GetData<MatchData>( matchName );
				await liteDb.SaveData( matchData );
			}



		}
	}
}
