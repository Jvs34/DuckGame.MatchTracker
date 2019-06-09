using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MatchTracker
{
	public class HttpGameDatabase : IGameDatabase
	{
		protected HttpClient Client { get; }

		public virtual bool ReadOnly => true;

		public SharedSettings SharedSettings { get; set; } = new SharedSettings();

		protected JsonSerializer Serializer { get; } = new JsonSerializer()
		{
			Formatting = Formatting.Indented ,
			PreserveReferencesHandling = PreserveReferencesHandling.Objects ,
		};

		public HttpGameDatabase( HttpClient httpClient )
		{
			Client = httpClient;
		}

		public virtual async Task Load()
		{
			await Task.CompletedTask;
		}

		public virtual async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			await Task.CompletedTask;
		}

		public virtual async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			T data = default;

			string url = SharedSettings.GetDataPath<T>( dataId , true );

			if( !string.IsNullOrEmpty( url ) )
			{
				try
				{
					var responseStream = await Client.GetStreamAsync( url );

					using( StreamReader reader = new StreamReader( responseStream ) )
					using( JsonTextReader jsonReader = new JsonTextReader( reader ) )
					{
						data = Serializer.Deserialize<T>( jsonReader );
					}
				}
				catch( HttpRequestException e )
				{
					Console.WriteLine( e );
					System.Diagnostics.Debug.WriteLine( e );
				}
			}

			return data;
		}
	}
}
