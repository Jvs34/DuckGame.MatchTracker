using Google.Apis.Util.Store;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchUploader
{
	//this is kind of horrible atm since it's storing json string inside of json but I am following the IDataStore implementation correctly at least, for now
	public class KeyValueDataStore : IDataStore
	{
		public Dictionary<string , string> Data { get; set; } = new Dictionary<string , string>();
		//FileDataStore does it

		public async Task ClearAsync()
		{
			await Task.CompletedTask;
			Data.Clear();

		}

		public async Task DeleteAsync<T>( string key )
		{
			await Task.CompletedTask;
			Data.Remove( key );
		}

		public async Task<T> GetAsync<T>( string key )
		{
			await Task.CompletedTask;

			if( string.IsNullOrEmpty( key ) )
			{
				throw new ArgumentException( "Key MUST have a value" );
			}

			if( Data.ContainsKey( key ) )
			{
				return JsonConvert.DeserializeObject<T>( Data.GetValueOrDefault( key ) );
			}

			return default;
		}

		public async Task StoreAsync<T>( string key , T value )
		{
			await Task.CompletedTask;

			var convertedValue = JsonConvert.SerializeObject( value );

			if( Data.ContainsKey( key ) )
			{
				Data [key] = convertedValue;
			}
			else
			{
				Data.Add( key , convertedValue );
			}
		}
	}
}