using Newtonsoft.Json;
using Octokit;
using Octokit.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class OctoKitGameDatabase : HttpGameDatabase
	{
		private GitHubClient OctoKitClient { get; }

		private OctoKitHttpClient GithubHttpClient { get; }

		public override bool ReadOnly => OctoKitClient.Credentials == null;

		public OctoKitGameDatabase( HttpClient http , string username = "" , string password = "" ) : base( http )
		{
			GithubHttpClient = new OctoKitHttpClient( Client );
			OctoKitClient = new GitHubClient( new Connection( new ProductHeaderValue( "MatchTracker" ) , GithubHttpClient ) );

			if( !string.IsNullOrEmpty( username ) && !string.IsNullOrEmpty( password ) )
			{
				OctoKitClient.Credentials = new Credentials( username , password );
			}
		}

		/*
		protected override async Task LoadZippedDatabase()
		{
			byte [] archive = null;

			try
			{
				archive = await OctoKitClient.Repository.Content.GetArchive( SharedSettings.RepositoryUser , SharedSettings.RepositoryName );
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
				Debug.WriteLine( e );
			}

			if( archive == null )
			{
				return;
			}

			using( var zipStream = new MemoryStream( archive ) )
			using( ZipArchive zipArchive = new ZipArchive( zipStream , ZipArchiveMode.Read ) )
			{

			}
		}
		*/

		public override async Task SaveData<T>( T data )
		{
			if( ReadOnly )
			{
				throw new Exception( "Cannot save data if user is unauthenticated" );
			}

			string url = SharedSettings.GetDataPath<T>( data.DatabaseIndex , true );

			url = url.Replace( SharedSettings.BaseRepositoryUrl , string.Empty );

			var newFileContent = new StringBuilder();

			using( StringWriter writer = new StringWriter( newFileContent ) )
			{
				Serialize( data , writer );
			}

			IReadOnlyList<RepositoryContent> fileContents = null;

			try
			{
				//I don't know why this would throw an exception instead of just giving an empty list but WHATEVER MAN
				fileContents = await OctoKitClient.Repository.Content.GetAllContents( SharedSettings.RepositoryUser , SharedSettings.RepositoryName , url );

				if( fileContents?.Count > 0 )
				{
					var fileInfo = fileContents [0];

					var result = await OctoKitClient.Repository.Content.UpdateFile(
						SharedSettings.RepositoryUser ,
						SharedSettings.RepositoryName ,
						url ,
						new UpdateFileRequest( $"Updated {typeof( T )} : {data.DatabaseIndex}" , newFileContent.ToString() , fileInfo.Sha , true )
					);
				}
			}
			catch( NotFoundException )
			{
				//the file was not found, this is totally fine, we'll create it now
				var result = await OctoKitClient.Repository.Content.CreateFile(
					SharedSettings.RepositoryUser ,
					SharedSettings.RepositoryName ,
					url ,
					new CreateFileRequest( $"Created {typeof( T )} : {data.DatabaseIndex}" , newFileContent.ToString() , true )
				);
			}

		}
	}
}
