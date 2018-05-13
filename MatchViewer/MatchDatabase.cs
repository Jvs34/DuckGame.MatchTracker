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
			globalData = null;
			matchData = new Dictionary<string , MatchData>();
			roundData = new Dictionary<string , RoundData>();
		}
	}

	public class MatchDatabase : IMatchDatabase
	{
		private HttpClient client;
		private SharedSettings sharedSettings;
		private CachedMatchDatabase cachedData;
		private bool loadedSettings;

		public MatchDatabase( HttpClient givenClient )
		{
			client = givenClient;
			//baseRepositoryUrl = "https://raw.githubusercontent.com/Jvs34/DuckGame.MatchDB/master/";
			//find a way to load the sharedssettings from the root
			sharedSettings = new SharedSettings();
			loadedSettings = false;
			cachedData = new CachedMatchDatabase();

			//UGH:I hate this shit so much, this might have some race condition, but hopefully since this is an injected singleton, it'll be done loading
			//by the time it's needed, if shit doesn't work then point fingers at this
		}

		public async Task LoadSettings()
		{
			if( loadedSettings )
				return;

			//TODO: THIS FILE IS NOT SYNCED ONE WAY TO THE WWWROOT FOLDER YET!!!!
			Console.WriteLine( "Requesting shared.json" );
			sharedSettings = await client.GetJsonAsync<SharedSettings>( "/shared.json" );
			loadedSettings = true;
		}

		public String GetRepositoryUrl()
		{
			return sharedSettings.baseRepositoryUrl;
		}

		public async Task LoadAllData()
		{
			await LoadSettings();
			await GetGlobalData();
			foreach( String matchName in cachedData.globalData.matches )
			{
				await GetMatchData( matchName );
			}

			foreach( String roundName in cachedData.globalData.rounds )
			{
				await GetRoundData( roundName );
			}
		}


		private void CacheGlobalData( GlobalData globalData )
		{
			cachedData.globalData = globalData;
		}

		private void CacheMatchData( String matchName , MatchData matchData )
		{
			if( !cachedData.matchData.ContainsKey( matchName ) )
			{
				cachedData.matchData.Add( matchName , matchData );
			}
		}

		private void CacheRoundData( String roundName , RoundData roundData )
		{
			if( !cachedData.roundData.ContainsKey( roundName ) )
			{
				cachedData.roundData.Add( roundName , roundData );
			}
		}

		public async Task<GlobalData> GetGlobalData()
		{
			await LoadSettings();

			GlobalData globalData;
			if( cachedData.globalData != null )
			{
				return cachedData.globalData;
			}
			else
			{
				String globalDataUrl = GetGlobalUrl();
				Console.WriteLine( "Requesting {0}" , globalDataUrl );
				globalData = await client.GetJsonAsync<GlobalData>( globalDataUrl );
				CacheGlobalData( globalData );
			}

			return globalData;
		}

		public async Task<MatchData> GetMatchData( string matchName )
		{
			await LoadSettings();

			MatchData matchData;
			if( cachedData.matchData.ContainsKey( matchName ) && cachedData.matchData.TryGetValue( matchName , out matchData ) )
			{
				return matchData;
			}
			else
			{
				String matchDataUrl = GetMatchUrl( matchName );
				Console.WriteLine( "Requesting {0} " , matchDataUrl );
				matchData = await client.GetJsonAsync<MatchData>( matchDataUrl );
				CacheMatchData( matchName , matchData );
			}
			return matchData;

		}

		public async Task<RoundData> GetRoundData( string roundName )
		{
			await LoadSettings();

			RoundData roundData;
			if( cachedData.roundData.ContainsKey( roundName ) && cachedData.roundData.TryGetValue( roundName , out roundData ) )
			{
				return roundData;
			}
			else
			{
				String roundDataUrl = GetRoundUrl( roundName );
				Console.WriteLine( "Requesting {0} " , roundDataUrl );
				roundData = await client.GetJsonAsync<RoundData>( roundDataUrl );
				CacheRoundData( roundName , roundData );
			}
			return roundData;
		}

		public String GetGlobalUrl()
		{
			return Url.Combine( GetRepositoryUrl() , sharedSettings.globalDataFile );
		}

		public String GetMatchUrl( String matchName )
		{
			String matchFolder = Url.Combine( GetRepositoryUrl() , sharedSettings.matchesFolder );
			String matchPath = Url.Combine( matchFolder , matchName + ".json" );
			return matchPath;//Url.Combine( matchPath , ".json" );//this would try to add /.json so don't do it here
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
