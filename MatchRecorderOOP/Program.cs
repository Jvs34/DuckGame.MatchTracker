using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace MatchRecorderOOP
{
	class Program
	{
		static async Task Main( string [] args )
		{
			using var recorderHandler = new MatchRecorder.MatchRecorderServer( Directory.GetCurrentDirectory() );
		}
	}
}
