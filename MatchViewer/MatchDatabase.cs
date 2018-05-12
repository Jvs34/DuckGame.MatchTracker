using System;
using System.Net.Http;
using System.Threading.Tasks;
using MatchTracker;
using Flurl;
using Microsoft.AspNetCore.Blazor;
using System.Collections.Generic;
using System.Linq;

namespace MatchViewer
{
	public interface IMatchDatabase
	{
		Task<GlobalData> GetGlobalData();

		Task<MatchData> GetMatchData( String matchName );

		Task<RoundData> GetRoundData( String roundName );

		int GetMatchesCount();
		int GetRoundsCount();


		Task LoadAllData();
	}

	class CachedMatchDatabase
	{
		public GlobalData globalData;
		public Dictionary<String , MatchData> matchData;
		public Dictionary<String , RoundData> roundData;
		public CachedMatchDatabase()
		{
			globalData = new GlobalData();
			matchData = new Dictionary<string , MatchData>();
			roundData = new Dictionary<string , RoundData>();
		}

		/*
		public int GetCachedProgress()
		{
			int matchProgress = 0;
			int roundProgress = 0;

			int matchCount = globalData.matches.Count;
			int roundCount = globalData.rounds.Count;

			if( matchCount == 0 || roundCount == 0 )
				return 0;

			int currentMatchesCount = globalData.matches.Count( str => matchData.ContainsKey( str ) );
			int currentRoundsCount = globalData.rounds.Count( str => roundData.ContainsKey( str ) );

			//250:13 = 50:x
			matchProgress = ( currentMatchesCount * 50 ) / matchCount;
			roundProgress = ( currentRoundsCount * 50 ) / roundCount;
			return matchProgress + roundProgress;
		}
		*/
	}

	public class MatchDatabase : IMatchDatabase
	{
		private HttpClient client;
		private String baseRepositoryUrl;
		private SharedSettings sharedSettings;
		private CachedMatchDatabase cachedData;

		public MatchDatabase( HttpClient givenClient )
		{
			client = givenClient;
			baseRepositoryUrl = "https://raw.githubusercontent.com/Jvs34/DuckGame.MatchDB/master/";
			//find a way to load the sharedssettings from the root
			sharedSettings = new SharedSettings();
			cachedData = new CachedMatchDatabase();
			LoadSettings();
		}

		public async void LoadSettings()
		{

			Console.WriteLine( "Requesting shared.json" );
			sharedSettings = await client.GetJsonAsync<SharedSettings>( "/shared.json" );
		}

		public String GetRepositoryUrl()
		{
			return baseRepositoryUrl;
		}

		public async Task LoadAllData()
		{
			cachedData.globalData = await GetGlobalData();
			foreach( String matchName in cachedData.globalData.matches )
			{
				MatchData matchData = await GetMatchData( matchName );
				var matchList = cachedData.matchData;
				matchList.Add( matchName , matchData );
			}

			foreach( String roundName in cachedData.globalData.rounds )
			{
				RoundData roundData = await GetRoundData( roundName );
				var roundList = cachedData.roundData;
				roundList.Add( roundName , roundData );
			}
		}

		
		public async Task<GlobalData> GetGlobalData()
		{
			String globalDataUrl = GetGlobalUrl();
			Console.WriteLine( "Requesting {0}" , globalDataUrl );
			return await client.GetJsonAsync<GlobalData>( globalDataUrl );
		}

		public async Task<MatchData> GetMatchData( string matchName )
		{
			String matchDataUrl = GetMatchUrl( matchName );
			Console.WriteLine( "Requesting {0} " , matchDataUrl );
			return await client.GetJsonAsync<MatchData>( matchDataUrl );
		}

		public async Task<RoundData> GetRoundData( string roundName )
		{
			String roundDataUrl = GetRoundUrl( roundName );
			Console.WriteLine( "Requesting {0} " , roundDataUrl );
			return await client.GetJsonAsync<RoundData>( roundDataUrl );
		}

		public String GetGlobalUrl()
		{
			return Url.Combine( GetRepositoryUrl() , sharedSettings.globalDataFile );
		}

		public String GetMatchUrl( String matchName )
		{
			String matchFolder = Url.Combine( GetRepositoryUrl() , sharedSettings.matchesFolder );
			String matchPath = Url.Combine( matchFolder , matchName + ".json" );
			return matchPath;//Url.Combine( matchPath , ".json" );
		}

		public String GetRoundUrl( String roundName )
		{
			String roundFolder = Url.Combine( GetRepositoryUrl() , sharedSettings.roundsFolder );
			String roundFile = Url.Combine( roundFolder , roundName );
			return Url.Combine( roundFile , sharedSettings.roundDataFile );
		}

		public int GetMatchesCount()
		{
			return cachedData.matchData.Count;
		}

		public int GetRoundsCount()
		{
			return cachedData.roundData.Count;
		}
	}
}
