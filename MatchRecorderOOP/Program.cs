using Steamworks;
using System;
using System.Threading.Tasks;

namespace MatchRecorderOOP
{
	class Program
	{
		static async Task Main( string [] args )
		{
			SteamClient.Init( 312530 );

			Console.WriteLine( "Hello World!" );

			Console.ReadLine();
		}
	}
}
