using MatchTracker;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MatchTest
{
	public class TestGithub
	{

		public async Task Run()
		{
			var Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "shared.json" )
				.AddJsonFile( "uploader.json" )
			.Build();


			HttpClient httpClient = new HttpClient();

			IGameDatabase db = new OctoKitGameDatabase( httpClient , Configuration ["GitUsername"] , Configuration ["GitPassword"] )
			{
				InitialLoad = true ,
			};
			Configuration.Bind( db.SharedSettings );

			await db.Load();


		}
	}
}
