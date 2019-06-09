using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MatchTest
{
	internal static class Program
	{


		private static async Task Main( string [] args )
		{
			//await new TestEF().Test();
			//await new TestYoutubeExplode().Test();
			//await new TestLiteDB().Test();
			//await new TestReplay().Test();

			await new TestGithub().Run();

			//await new TestFS().Run();

			await Task.CompletedTask;
			Console.ReadLine();
		}


	}
}