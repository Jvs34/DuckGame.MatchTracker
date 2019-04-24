using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MatchTest
{
	public class TestEF
	{
		private static async Task<GlobalData> LoadDatabaseGlobalDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings )
		{
			return JsonConvert.DeserializeObject<GlobalData>( await File.ReadAllTextAsync( sharedSettings.GetGlobalPath() ) );
		}

		private static async Task<MatchData> LoadDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string matchName )
		{
			return JsonConvert.DeserializeObject<MatchData>( await File.ReadAllTextAsync( sharedSettings.GetMatchPath( matchName ) ) );
		}

		private static async Task<RoundData> LoadDatabaseRoundDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string roundName )
		{
			return JsonConvert.DeserializeObject<RoundData>( await File.ReadAllTextAsync( sharedSettings.GetRoundPath( roundName ) ) );
		}
		public async Task Test()
		{
			IGameDatabase gameDatabase = new EFGameDatabase();
			gameDatabase.LoadGlobalDataDelegate += LoadDatabaseGlobalDataFile;
			gameDatabase.LoadMatchDataDelegate += LoadDatabaseMatchDataFile;
			gameDatabase.LoadRoundDataDelegate += LoadDatabaseRoundDataFile;
			gameDatabase.SaveGlobalDataDelegate += SaveDatabaseGlobalDataFile;
			gameDatabase.SaveMatchDataDelegate += SaveDatabaseMatchDataFile;
			gameDatabase.SaveRoundDataDelegate += SaveDatabaseRoundataFile;

			var Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared_debug.json" )
			.Build();

			Configuration.Bind( gameDatabase.SharedSettings );

			//RoundData roundData = await gameDatabase.GetRoundData( "2018-08-02 15-22-38" );
			RoundData roundData = await gameDatabase.GetRoundData( "2018-08-10 18-12-33" );

			Console.WriteLine( JsonConvert.SerializeObject( roundData , Formatting.Indented , new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects } ) );

			//GlobalData globalData = await gameDatabase.GetGlobalData();

			//Console.WriteLine( roundData.players.First()?.name );

			Console.ReadLine();
		}

		private static async Task SaveDatabaseGlobalDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , GlobalData globalData )
		{
			await File.WriteAllTextAsync( sharedSettings.GetGlobalPath() , JsonConvert.SerializeObject( globalData , Formatting.Indented ) );
		}

		private static async Task SaveDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , String matchName , MatchData matchData )
		{
			await File.WriteAllTextAsync( sharedSettings.GetMatchPath( matchName ) , JsonConvert.SerializeObject( matchData , Formatting.Indented ) );
		}

		private static async Task SaveDatabaseRoundataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , String roundName , RoundData roundData )
		{
			await File.WriteAllTextAsync( sharedSettings.GetRoundPath( roundName ) , JsonConvert.SerializeObject( roundData , Formatting.Indented ) );
		}
	}
}
