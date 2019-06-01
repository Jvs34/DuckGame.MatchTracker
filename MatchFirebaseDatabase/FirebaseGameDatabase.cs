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
		public SharedSettings SharedSettings { get; set; }

		public bool ReadOnly => false;

		public FirebaseClient FirebaseClient { get; private set; }

		private HttpClient HttpClient { get; }

		public FirebaseSettings FirebaseSettings { get; set; } = new FirebaseSettings();

		public FirebaseGameDatabase( HttpClient httpClient = null )
		{
			HttpClient = httpClient;
		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			CheckDatabase();
			var collection = FirebaseClient.Child( typeof( T ).Name ).Child( string.IsNullOrEmpty( dataId ) ? typeof( T ).Name : dataId );
			return await collection.OnceSingleAsync<T>();
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback )
		{
			CheckDatabase();
			throw new NotImplementedException( "IterateOverAllRoundsOrMatches needs a more sensible way to be implemented quite honestly" );
		}

		public async Task Load()
		{
			if( FirebaseClient == null )
			{
				FirebaseClient = new FirebaseClient( FirebaseSettings.FirebaseURL , new FirebaseOptions()
				{
					HttpClientFactory = this ,
					JsonSerializerSettings = new JsonSerializerSettings()
					{
						Formatting = Formatting.Indented
					} ,
					AuthTokenAsyncFactory = () => Task.FromResult( FirebaseSettings.FirebaseURL ) ,
				} );

				await GetData<GlobalData>();
			}
		}

		private void CheckDatabase()
		{
			if( FirebaseClient == null )
			{
				throw new NullReferenceException( "Database was not loaded, please call FirebaseGameDatabase.Load first!" );
			}
		}

		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			CheckDatabase();

			var collection = FirebaseClient.Child( typeof( T ).Name ).Child( data.DatabaseIndex );

			await collection.PostAsync( data , false );
		}

		public IHttpClientProxy GetHttpClient( TimeSpan? timeout ) => this;

		public HttpClient GetHttpClient() => HttpClient ?? new HttpClient();

		public void Dispose()
		{
		}
	}
}
