using System;
using System.Net.Http;
using System.Threading.Tasks;
using MatchTracker;
using Flurl;
using Microsoft.AspNetCore.Blazor;

namespace MatchViewer
{
	public interface IMatchDatabase
	{
		Task<GlobalData> GetGlobalData();

		Task<MatchData> GetMatchData( String matchName );

		Task<RoundData> GetRoundData( String roundName );
	}

	public class MatchDatabase : IMatchDatabase
	{
		private HttpClient client;
		private String baseRepositoryUrl;
		private SharedSettings sharedSettings;

		public MatchDatabase( HttpClient givenClient )
		{
			client = givenClient;
			baseRepositoryUrl = "https://raw.githubusercontent.com/Jvs34/DuckGame.MatchDB/master/";
			//find a way to load the sharedssettings from the root
			sharedSettings = new SharedSettings();
			LoadSettings();
		}

		public async void LoadSettings()
		{
			sharedSettings = await client.GetJsonAsync<SharedSettings>( "/shared.json" );
		}

		public String GetRepositoryUrl()
		{
			return baseRepositoryUrl;
		}

		public async Task<GlobalData> GetGlobalData()
		{
			String globalDataUrl = GetGlobalUrl();
			Console.WriteLine( globalDataUrl );
			return await client.GetJsonAsync<GlobalData>( globalDataUrl );
		}

		public async Task<MatchData> GetMatchData( string matchName )
		{
			String matchDataUrl = GetMatchUrl( matchName );
			Console.WriteLine( matchDataUrl );
			return await client.GetJsonAsync<MatchData>( matchDataUrl );
		}

		public async Task<RoundData> GetRoundData( string roundName )
		{
			String roundDataUrl = GetRoundUrl( roundName );
			Console.WriteLine( roundDataUrl );

			return await client.GetJsonAsync<RoundData>( roundDataUrl );
		}

		public String GetGlobalUrl()
		{
			return Url.Combine( GetRepositoryUrl() , sharedSettings.globalDataFile );
		}

		public String GetMatchUrl( String matchName )
		{
			String matchFolder = Url.Combine( GetRepositoryUrl() , sharedSettings.matchesFolder );
			String matchPath = Url.Combine( matchFolder , matchName );
			return Url.Combine( matchPath , ".json" );
		}

		public String GetRoundUrl( String roundName )
		{
			String roundFolder = Url.Combine( GetRepositoryUrl() , sharedSettings.roundsFolder );
			String roundFile = Url.Combine( roundFolder , roundName );
			return Url.Combine( roundFile , sharedSettings.roundDataFile );
		}
	}
}
