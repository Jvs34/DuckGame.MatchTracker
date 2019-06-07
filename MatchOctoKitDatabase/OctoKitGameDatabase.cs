using Newtonsoft.Json;
using Octokit;
using Octokit.Internal;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class OctoKitGameDatabase : IGameDatabase
	{
		private GitHubClient Client { get; }

		private HttpClient HttpClient { get; }

		private OctoKitHttpClient GithubHttpClient { get; }

		public SharedSettings SharedSettings { get; set; } = new SharedSettings();

		public bool ReadOnly => Client.Credentials == null;

		public string RepositoryName => SharedSettings.RepositoryName;

		public string RepositoryUser => SharedSettings.RepositoryUser;

		private JsonSerializer Serializer { get; } = new JsonSerializer()
		{
			Formatting = Formatting.Indented ,
			PreserveReferencesHandling = PreserveReferencesHandling.Objects ,
		};

		public OctoKitGameDatabase( HttpClient http , string username = "" , string password = "" )
		{
			HttpClient = http;
			GithubHttpClient = new OctoKitHttpClient( HttpClient );
			Client = new GitHubClient( new Connection( new ProductHeaderValue( "MatchTracker" ) , GithubHttpClient ) );

			if( !string.IsNullOrEmpty( username ) && !string.IsNullOrEmpty( password ) )
			{
				Client.Credentials = new Credentials( username , password );
			}
		}

		public async Task Load()
		{
			//var gay = await Client.User.Current();
			//var repo = await Client.Repository.Get( RepositoryUser , RepositoryName );
		}

		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			if( ReadOnly )
			{
				throw new Exception( "Cannot save data if user is unauthenticated" );
			}

			string url = SharedSettings.GetDataPath<T>( data.DatabaseIndex , true );

			url = url.Replace( SharedSettings.BaseRepositoryUrl , string.Empty );

			var file = await Client.Repository.Content.GetAllContents( RepositoryUser , RepositoryName , url );

			var newFileContent = new StringBuilder();

			using( StringWriter writer = new StringWriter( newFileContent ) )
			{
				Serializer.Serialize( writer , data );
			}

			//if the file doesn't exist, we need to create it instead
			if( file.Count == 0 )
			{
				var result = await Client.Repository.Content.CreateFile(
					RepositoryUser ,
					RepositoryName ,
					url ,
					new CreateFileRequest( $"Created {typeof( T )} : {data.DatabaseIndex}" , newFileContent.ToString() )
				);
			}
			else
			{
				var fileInfo = file [0];

				//update it instead

				var result = await Client.Repository.Content.UpdateFile(
					RepositoryUser ,
					RepositoryName ,
					url ,
					new UpdateFileRequest( $"Updated {typeof( T )} : {data.DatabaseIndex}" , newFileContent.ToString() , fileInfo.Sha )
				);
			}
		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			T data = default;

			string url = SharedSettings.GetDataPath<T>( dataId , true );

			if( !string.IsNullOrEmpty( url ) )
			{
				var responseStream = await HttpClient.GetStreamAsync( url );

				using( StreamReader reader = new StreamReader( responseStream ) )
				using( JsonTextReader jsonReader = new JsonTextReader( reader ) )
				{
					data = Serializer.Deserialize<T>( jsonReader );
				}
			}

			return data;
		}
	}
}
