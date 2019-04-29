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
			IGameDatabase defaultDatabase = new GameDatabase();
			defaultDatabase.LoadGlobalDataDelegate += async ( _ , ss ) => JsonConvert.DeserializeObject<GlobalData>( await File.ReadAllTextAsync( ss.GetGlobalPath() ) );
			defaultDatabase.LoadMatchDataDelegate += async ( _ , ss , matchName ) => JsonConvert.DeserializeObject<MatchData>( await File.ReadAllTextAsync( ss.GetMatchPath( matchName ) ) );
			defaultDatabase.LoadRoundDataDelegate += async ( _ , ss , roundName ) => JsonConvert.DeserializeObject<RoundData>( await File.ReadAllTextAsync( ss.GetRoundPath( roundName ) ) );

			IGameDatabase liteDb = new LiteDBGameDatabase()
			{
				FilePath = @"E:\Test\duckgame.db" ,
			};

			var Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
			.Build();

			Configuration.Bind( defaultDatabase.SharedSettings );
			Configuration.Bind( liteDb.SharedSettings );

			await defaultDatabase.Load();

			await liteDb.Load();

			GlobalData globalData = await defaultDatabase.GetGlobalData();

			await liteDb.SaveGlobalData( globalData );


			foreach( var roundName in globalData.Rounds )
			{
				RoundData roundData = await defaultDatabase.GetRoundData( roundName );
				await liteDb.SaveRoundData( roundName , roundData );
			}

			foreach( var matchName in globalData.Matches )
			{
				MatchData matchData = await defaultDatabase.GetMatchData( matchName );
				await liteDb.SaveMatchData( matchName , matchData );
			}


			GlobalData gaydata = await liteDb.GetGlobalData();

			RoundData randomRound = await liteDb.GetRoundData( globalData.Rounds [globalData.Rounds.Count / 2] );
		}
	}
}
