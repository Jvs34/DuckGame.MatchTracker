using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MatchUploader
{
	public static class Program
	{
		public static async Task Main( string [] args )
		{
			using var uploader = new MatchUploaderHandler( args );

			await uploader.RunAsync();
		}
	}
}