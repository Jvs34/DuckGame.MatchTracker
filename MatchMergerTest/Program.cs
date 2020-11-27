using System;
using System.Threading.Tasks;

namespace MatchMergerTest
{
	class Program
	{
		static async Task Main( string [] args )
		{
			var job = new MergingJob();
			await job.RunAsync();
			Console.ReadLine();
		}
	}
}
