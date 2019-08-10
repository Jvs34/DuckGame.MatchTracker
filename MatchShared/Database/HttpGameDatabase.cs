using Flurl;
using System;
using System.IO;
using System.IO.Compression;
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

		protected virtual async Task<Stream> GetZippedDatabaseStream()
		{
			//TODO: unhardcode this to not use github
			string repositoryZipUrl = Url.Combine( "https://github.com" , SharedSettings.RepositoryUser , SharedSettings.RepositoryName , "archive" , "master.zip" );

			var httpResponse = await Client.GetAsync( repositoryZipUrl , HttpCompletionOption.ResponseHeadersRead );

			if( httpResponse.IsSuccessStatusCode )
			{
				return await httpResponse.Content.ReadAsStreamAsync();
			}

			return null;
		}

		public override async Task SaveData<T>( T data )
		{
			await Task.CompletedTask;
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
				System.Diagnostics.Debug.WriteLine( e );
			}

			return data;
		}

	}
}
