using MatchTracker;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MatchTest
{
	internal static class Program
	{
		private static async Task Main( string [] args )
		{
			/*
			var httpClient = new HttpClient();

			HttpGameDatabase db = new HttpGameDatabase( httpClient );
			db.SharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( Path.Combine( "Settings" , "shared.json" ) ) );
			string roundName = "2019-05-14 22-20-29";

			var path = db.SharedSettings.GetRoundVideoPath( roundName );


			Console.WriteLine( File.Exists( path ) );
			*/
			await new CopyToUnity().Run();
		}
	}
}