using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Newtonsoft.Json;

namespace MatchUploader
{
	public class UploaderSettings
	{
		public String lastUploadToResume = null; //this is the round name itself, we can get the path to the video file later
		public Uri uploadToResume = null;
		public float uploadSpeed = 0; //in kylobytes per seconds, 0 means no throttling
		public ClientSecrets secrets;
		public KeyValueDataStore dataStore;
	}


	//this is kind of horrible atm since it's storing json string inside of json but I am following the IDataStore implementation correctly at least, for now
	public class KeyValueDataStore : IDataStore
	{
		
		public Dictionary<String , String> data;
		private static readonly Task CompletedTask = Task.FromResult( 0 ); //FileDataStore does it


		public KeyValueDataStore()
		{
			data = new Dictionary<string , string>();
		}

		public Task ClearAsync()
		{
			data.Clear();

			return CompletedTask;
		}

		public Task DeleteAsync<T>( string key )
		{
			data.Remove( key );

			return CompletedTask;
		}

		public Task<T> GetAsync<T>( string key )
		{
			if( string.IsNullOrEmpty( key ) )
			{
				throw new ArgumentException( "Key MUST have a value" );
			}

			TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

			bool contains = data.ContainsKey( key );

			if( contains )
			{
				var value = data;
				tcs.SetResult( JsonConvert.DeserializeObject<T>( data.GetValueOrDefault( key ) ) );
			}
			else
			{
				tcs.SetResult( default( T ) );
			}
			return tcs.Task;
		}

		public Task StoreAsync<T>( string key , T value )
		{
			data.Add( key , JsonConvert.SerializeObject( value ) );
			return CompletedTask;
		}

	}
}
