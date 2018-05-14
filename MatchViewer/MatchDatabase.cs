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

		private Task settingsTask;
		private Task<GlobalData> globalDataTask;
		private Dictionary<String , Task<MatchData>> matchDataTasks;
		private Dictionary<String , Task<RoundData>> roundDataTasks;

		public MatchDatabase( HttpClient givenClient )
		{
			client = givenClient;
			//baseRepositoryUrl = "https://raw.githubusercontent.com/Jvs34/DuckGame.MatchDB/master/";
			//find a way to load the sharedssettings from the root
			sharedSettings = new SharedSettings();
			cachedData = new CachedMatchDatabase();
			settingsTask = LoadSettings();
			matchDataTasks = new Dictionary<String , Task<MatchData>>();
			roundDataTasks = new Dictionary<String , Task<RoundData>>();
		}

		public async Task LoadSettings()
		{
			//TODO: THIS FILE IS NOT SYNCED ONE WAY TO THE WWWROOT FOLDER YET!!!!
			Console.WriteLine( "Requesting shared.json" );
			sharedSettings = await client.GetJsonAsync<SharedSettings>( "/shared.json" );
		}


		public async Task LoadAllData()
		{
			await settingsTask.ConfigureAwait( false );
			//await LoadSettings();
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

		private bool HasGlobalDataCache()
		{
			return cachedData.globalData != null;
		}

		private void CacheMatchData( String matchName , MatchData matchData )
		{
			if( !cachedData.matchData.ContainsKey( matchName ) )
			{
				cachedData.matchData.Add( matchName , matchData );
			}
		}

		private bool HasMatchDataCache( String matchName )
		{
			return cachedData.matchData.ContainsKey( matchName );
		}

		private void CacheRoundData( String roundName , RoundData roundData )
		{
			if( !cachedData.roundData.ContainsKey( roundName ) )
			{
				cachedData.roundData.Add( roundName , roundData );
			}
		}

		private bool HasRoundDataCache( String roundName )
		{
			return cachedData.roundData.ContainsKey( roundName );
		}

		public async Task<GlobalData> GetGlobalData()
		{
			//if there is cached data, just return it
			//if there is no cached data and there is a pending task, just wait the task
			//if there is no cached data, make the http call and save the task

			GlobalData globalData;
			if( HasGlobalDataCache() || globalDataTask != null )
			{
				Console.WriteLine( "Waiting for cached globaldata" );
				if( globalDataTask != null )
				{
					await globalDataTask;
				}
				globalData = cachedData.globalData;
			}
			else
			{
				globalDataTask = GetInternalGlobalData();
				globalData = await globalDataTask.ConfigureAwait( false );
				CacheGlobalData( globalData );
			}
			return globalData;
		}

		public async Task<MatchData> GetMatchData( string matchName )
		{
			MatchData matchData;

			if( HasMatchDataCache( matchName ) || matchDataTasks.ContainsKey( matchName ) )
			{
				Console.WriteLine( "Waiting for cached matchdata" );
				if( matchDataTasks.TryGetValue( matchName , out Task<MatchData> task ) )
				{
					await task;
				}

				cachedData.matchData.TryGetValue( matchName , out matchData );
			}
			else
			{
				var task = GetInternalMatchData( matchName );
				matchDataTasks.Add( matchName , task );
				matchData = await task.ConfigureAwait( false );
				CacheMatchData( matchName , matchData );
			}
			return matchData;
		}

		public async Task<RoundData> GetRoundData( string roundName )
		{
			RoundData roundData;

			if( HasRoundDataCache( roundName ) || roundDataTasks.ContainsKey( roundName ) )
			{
				Console.WriteLine( "Waiting for cached rounddata" );
				if( roundDataTasks.TryGetValue( roundName , out Task<RoundData> task ) )
				{
					await task;
				}

				cachedData.roundData.TryGetValue( roundName , out roundData );
			}
			else
			{
				var task = GetInternalRoundData( roundName );
				roundDataTasks.Add( roundName , task );
				roundData = await task.ConfigureAwait( false );
				CacheRoundData( roundName , roundData );
			}
			return roundData;
		}


		async Task<GlobalData> GetInternalGlobalData()
		{
			await settingsTask.ConfigureAwait( false );

			GlobalData globalData;
			String globalDataUrl = GetGlobalUrl();
			Console.WriteLine( "Requesting {0}" , globalDataUrl );
			globalData = await client.GetJsonAsync<GlobalData>( globalDataUrl );
			return globalData;
		}

		async Task<MatchData> GetInternalMatchData( string matchName )
		{
			await settingsTask.ConfigureAwait( false );

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

		async Task<RoundData> GetInternalRoundData( string roundName )
		{
			await settingsTask.ConfigureAwait( false );

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

		#region URLCRAP
		public String GetRepositoryUrl()
		{
			return sharedSettings.baseRepositoryUrl;
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
		#endregion
	}
}
