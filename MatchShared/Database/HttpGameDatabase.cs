using Flurl;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

		public virtual bool InitialLoad { get; set; } = false;

		public HttpGameDatabase( HttpClient httpClient )
		{
			Client = httpClient;
		}

		public override async Task Load()
		{
			//TODO:fetch and cache the entire database with a github download
			if( InitialLoad )
			{
				await LoadZippedDatabase();
			}
		}

		protected virtual async Task LoadZippedDatabase()
		{
			//SharedSettings.RepositoryUser , SharedSettings.RepositoryName
			string repositoryZipUrl = Url.Combine( "https://github.com" , SharedSettings.RepositoryUser , SharedSettings.RepositoryName , "archive" , "master.zip" );

			var httpResponse = await Client.GetAsync( repositoryZipUrl , HttpCompletionOption.ResponseHeadersRead );

			if( httpResponse.IsSuccessStatusCode )
			{
				string zipName = $"{SharedSettings.RepositoryName}-master";

				using( var responseStream = await httpResponse.Content.ReadAsStreamAsync() )
				using( ZipArchive zipArchive = new ZipArchive( responseStream , ZipArchiveMode.Read ) )
				{
					var globalPath = ToZipPath( zipName , SharedSettings.GetDataPath<GlobalData>( string.Empty ) );
					
					var globalDataStream = zipArchive.GetEntry( globalPath );
				}
			}
		}

		protected string ToZipPath( string zipName , string path )
		{
			return path.Replace( SharedSettings.BaseRecordingFolder , zipName ).Replace( "\\" , "/" );
		}

		public override async Task SaveData<T>( T data )
		{
			await Task.CompletedTask;
		}

		public override async Task<T> GetData<T>( string dataId = "" )
		{
			T data = GetCachedItem<T>( dataId );

			bool cacheExpired = IsCachedItemExpired<T>( dataId , DateTime.UtcNow );

			string url = SharedSettings.GetDataPath<T>( dataId , true );

			if( !string.IsNullOrEmpty( url ) && cacheExpired )
			{
				try
				{
					var httpResponse = await Client.GetAsync( url , HttpCompletionOption.ResponseHeadersRead );

					if( httpResponse.IsSuccessStatusCode )
					{
						using( var responseStream = await httpResponse.Content.ReadAsStreamAsync() )
						{
							data = Deserialize( responseStream , data );
						}

						SetCachedItem( data , httpResponse.Content.Headers.Expires.HasValue
							? httpResponse.Content.Headers.Expires.Value.DateTime
							: DateTime.UtcNow.AddSeconds( 60 ) );
					}
				}
				catch( HttpRequestException e )
				{
					Console.WriteLine( e );
					System.Diagnostics.Debug.WriteLine( e );
				}
			}

			return (T) data;
		}

	}
}
