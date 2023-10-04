using Google.Apis.Util.Store;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchUploader;

//this is kind of horrible atm since it's storing json string inside of json but I am following the IDataStore implementation correctly at least, for now
public class KeyValueDataStore : IDataStore
{
	public Dictionary<string , string> Data { get; set; } = new Dictionary<string , string>();

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
		return Data.ContainsKey( key ) ? JsonConvert.DeserializeObject<T>( Data [key] ) : default;
	}

	public async Task StoreAsync<T>( string key , T value )
	{
		await Task.CompletedTask;
		Data [key] = JsonConvert.SerializeObject( value );
	}
}