using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class FileSystemGameDatabase : GameDatabase
	{
		private JsonSerializerSettings JsonSettings { get; }

		public FileSystemGameDatabase()
		{

			JsonSettings = new JsonSerializerSettings()
			{
				PreserveReferencesHandling = PreserveReferencesHandling.Objects ,
			};


			LoadGlobalDataDelegate += LoadDatabaseGlobalDataFile;
			LoadMatchDataDelegate += LoadDatabaseMatchDataFile;
			LoadRoundDataDelegate += LoadDatabaseRoundDataFile;
			SaveGlobalDataDelegate += SaveDatabaseGlobalDataFile;
			SaveMatchDataDelegate += SaveDatabaseMatchDataFile;
			SaveRoundDataDelegate += SaveDatabaseRoundataFile;
		}

		private async Task<GlobalData> LoadDatabaseGlobalDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings )
		{
			await Task.CompletedTask;
			Console.WriteLine( "Loading GlobalData" );
			return JsonConvert.DeserializeObject<GlobalData>( File.ReadAllText( sharedSettings.GetGlobalPath() ) , JsonSettings );
		}

		private async Task<MatchData> LoadDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string matchName )
		{
			await Task.CompletedTask;
			Console.WriteLine( $"Loading MatchData {matchName}" );
			return JsonConvert.DeserializeObject<MatchData>( File.ReadAllText( sharedSettings.GetMatchPath( matchName ) ) , JsonSettings );
		}

		private async Task<RoundData> LoadDatabaseRoundDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string roundName )
		{
			await Task.CompletedTask;
			Console.WriteLine( $"Loading RoundData {roundName}" );
			return JsonConvert.DeserializeObject<RoundData>( File.ReadAllText( sharedSettings.GetRoundPath( roundName ) ) , JsonSettings );
		}

		private async Task SaveDatabaseGlobalDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , GlobalData globalData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetGlobalPath() , JsonConvert.SerializeObject( globalData , Formatting.Indented , JsonSettings ) );
		}

		private async Task SaveDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string matchName , MatchData matchData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetMatchPath( matchName ) , JsonConvert.SerializeObject( matchData , Formatting.Indented , JsonSettings ) );
		}

		private async Task SaveDatabaseRoundataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string roundName , RoundData roundData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetRoundPath( roundName ) , JsonConvert.SerializeObject( roundData , Formatting.Indented , JsonSettings ) );
		}
	}
}
