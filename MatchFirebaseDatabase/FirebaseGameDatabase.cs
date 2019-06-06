using Firebase;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class FirebaseGameDatabase : IGameDatabase, IHttpClientFactory, IHttpClientProxy
	{
		public SharedSettings SharedSettings { get; set; } = new SharedSettings();

		public bool ReadOnly => false;

		public FirebaseClient Client { get; private set; }

		private HttpClient HttpClient { get; }

		public FirebaseSettings FirebaseSettings { get; set; } = new FirebaseSettings();

		public FirebaseGameDatabase( HttpClient httpClient = null )
		{
			HttpClient = httpClient;
		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			CheckDatabase();
			var collection = Client.Child( typeof( T ).Name ).Child( string.IsNullOrEmpty( dataId ) ? typeof( T ).Name : dataId );
			return await collection.OnceSingleAsync<T>();
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task<bool>> callback )
		{
			CheckDatabase();

			if( callback == null )
				return;

			GlobalData globalData = await GetData<GlobalData>();

			foreach( string matchOrRoundName in matchOrRound ? globalData.Matches : globalData.Rounds )
			{
				IWinner iterateItem = matchOrRound ?
					await GetData<MatchData>( matchOrRoundName ) as IWinner :
					await GetData<RoundData>( matchOrRoundName ) as IWinner;

				bool shouldContinue = await callback( iterateItem );

				if( !shouldContinue )
				{
					break;
				}
			}
		}

		public async Task Load()
		{
			if( Client == null )
			{
				Client = new FirebaseClient( FirebaseSettings.FirebaseURL , new FirebaseOptions()
				{
					HttpClientFactory = this ,
					JsonSerializerSettings = new JsonSerializerSettings()
					{
						Formatting = Formatting.Indented
					} ,
					AuthTokenAsyncFactory = () => Task.FromResult( FirebaseSettings.FirebaseToken ) ,
				} );

				await GetData<GlobalData>();
			}
		}

		private void CheckDatabase()
		{
			if( Client == null )
			{
				throw new NullReferenceException( "Database was not loaded, please call FirebaseGameDatabase.Load first!" );
			}
		}

		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			CheckDatabase();

			var collection = Client.Child( typeof( T ).Name ).Child( data.DatabaseIndex );

			await collection.PostAsync( data , false );
		}

		public IHttpClientProxy GetHttpClient( TimeSpan? timeout ) => this;

		public HttpClient GetHttpClient() => HttpClient ?? new HttpClient();

		public void Dispose()
		{
		}
	}
}
