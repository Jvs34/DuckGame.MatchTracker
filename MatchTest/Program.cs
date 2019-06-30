using System;
using System.Threading.Tasks;

namespace MatchTest
{
	internal static class Program
	{


		private static async Task Main( string [] args )
		{
			await new CopyToUnity().Run();
		}


	}
}