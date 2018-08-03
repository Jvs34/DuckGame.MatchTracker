using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchUploader
{
	//this is kind of horrible atm since it's storing json string inside of json but I am following the IDataStore implementation correctly at least, for now
	public class KeyValueDataStore : IDataStore
	{
		private static readonly Task CompletedTask = Task.FromResult( 0 );
		public Dictionary<String , String> data { get; set; }
		//FileDataStore does it

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
			if( data.ContainsKey( key ) )
			{
				data [key] = JsonConvert.SerializeObject( value );
			}
			else
			{
				data.Add( key , JsonConvert.SerializeObject( value ) );
			}
			return CompletedTask;
		}
	}
}