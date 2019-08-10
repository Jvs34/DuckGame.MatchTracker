using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


		//protected override async Task<Stream> GetZippedDatabaseStream()
		//{
		//	return await base.GetZippedDatabaseStream();

		//	//ironically, this is actually worse than the default implementation because it uses precious hourly github api uses
		//	//but also because it has to be fully loaded before reading it

		//	/*
		//	byte [] archiveBytes = await OctoKitClient.Repository.Content.GetArchive( SharedSettings.RepositoryUser , SharedSettings.RepositoryName , ArchiveFormat.Zipball );

		//	if( archiveBytes != null )
		//	{
		//		return new MemoryStream( archiveBytes );
		//	}

		//	return null;
		//	*/
		//}


		public override async Task SaveData<T>( T data )
		{
			if( ReadOnly )
			{
				throw new Exception( "Cannot save data if user is unauthenticated" );
			}

			string url = SharedSettings.GetDataPath<T>( data.DatabaseIndex , true ).Replace( SharedSettings.BaseRepositoryUrl , string.Empty );

			var newFileContent = new StringBuilder();

			using( StringWriter writer = new StringWriter( newFileContent ) )
			{
				Serialize( data , writer );
			}

			IReadOnlyList<RepositoryContent> fileContents = new List<RepositoryContent>();

			try
			{
				//I don't know why this would throw an exception instead of just giving an empty list but WHATEVER MAN
				fileContents = await OctoKitClient.Repository.Content.GetAllContents( SharedSettings.RepositoryUser , SharedSettings.RepositoryName , url );
			}
			catch( Exception )
			{
				Console.WriteLine( $"Could not find {url}, creating." );
			}

			RepositoryContent fileInfo = fileContents.FirstOrDefault();

			UpdateFileRequest contentRequest = new UpdateFileRequest(
				$"{( fileInfo != null ? "Updated" : "Created" )} {typeof( T ).Name} : {data.DatabaseIndex}" ,
				newFileContent.ToString() ,
				fileInfo != null ? fileInfo.Sha : "null" ,
				true
			);

			if( fileInfo != null )
			{
				await OctoKitClient.Repository.Content.UpdateFile( SharedSettings.RepositoryUser , SharedSettings.RepositoryName , url , contentRequest );
			}
			else
			{
				//the file was not found, this is totally fine, we'll create it now
				await OctoKitClient.Repository.Content.CreateFile( SharedSettings.RepositoryUser , SharedSettings.RepositoryName , url , contentRequest );
			}

		}
	}
}
