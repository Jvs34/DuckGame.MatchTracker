using System.Threading.Tasks;

namespace MatchUploader
{
	public abstract class ModeHandler
	{
		public ModeHandler( string [] args )
		{

		}

		public abstract Task Run();
	}
}
