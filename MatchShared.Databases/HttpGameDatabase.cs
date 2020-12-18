using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class HttpGameDatabase : BaseGameDatabase
	{
		protected HttpClient Client { get; }

		public override bool ReadOnly => true;

		public HttpGameDatabase( HttpClient httpClient )
		{
			Client = httpClient;
		}

		public override async Task SaveData<T>( T data )
		{
			await Task.CompletedTask;
			if( ReadOnly )
			{
				throw new Exception( $"Cannot save data in {GetType()}, it is read only" );
			}
		}

		public override async Task<T> GetData<T>( string dataId = "" )
		{
			T data = default;

			string url = SharedSettings.GetDataPath<T>( dataId , true );

			try
			{
				using( var httpResponse = await Client.GetAsync( url , HttpCompletionOption.ResponseHeadersRead ) )
				{
					if( httpResponse.IsSuccessStatusCode )
					{
						var contentHeaders = httpResponse.Content.Headers;

						using( var responseStream = await httpResponse.Content.ReadAsStreamAsync() )
						{
							data = Deserialize<T>( responseStream );
						}

					}
				}
			}
			catch( HttpRequestException e )
			{
				Console.WriteLine( e );
			}

			return data;
		}

		public override void Dispose()
		{
		}
	}
}
