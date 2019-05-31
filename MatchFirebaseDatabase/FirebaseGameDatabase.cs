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

		private FirebaseClient FirebaseClient { get; }

		private HttpClient HttpClient { get; }

		public FirebaseGameDatabase( string firebaseUrl , HttpClient httpClient , string token )
		{
			HttpClient = httpClient;

			FirebaseClient = new FirebaseClient( firebaseUrl , new FirebaseOptions()
			{
				HttpClientFactory = this ,
				JsonSerializerSettings = new JsonSerializerSettings()
				{
					Formatting = Formatting.Indented
				} ,
				AuthTokenAsyncFactory = () => Task.FromResult( token ) ,
			} );

		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			var collection = FirebaseClient.Child( typeof( T ).Name ).Child( string.IsNullOrEmpty( dataId ) ? typeof( T ).Name : dataId );
			return await collection.OnceSingleAsync<T>();
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback )
		{
			throw new NotImplementedException();
		}

		public async Task Load()
		{
			await GetData<GlobalData>();
		}



		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			var collection = FirebaseClient.Child( typeof( T ).Name ).Child( data.DatabaseIndex );

			await collection.PostAsync( data , false );
		}

		public IHttpClientProxy GetHttpClient( TimeSpan? timeout ) => this;

		public HttpClient GetHttpClient() => HttpClient;

		public void Dispose()
		{
		}
	}
}
