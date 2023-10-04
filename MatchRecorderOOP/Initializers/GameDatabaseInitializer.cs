using Extensions.Hosting.AsyncInitialization;
using MatchTracker;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder.Initializers
{
	public sealed class GameDatabaseInitializer : IAsyncInitializer
	{
		public IGameDatabase Database { get; }

		public GameDatabaseInitializer( IOptions<SharedSettings> sharedSettings , IGameDatabase db )
		{
			Database = db;
			Database.SharedSettings = sharedSettings.Value;
		}

		public async Task InitializeAsync( CancellationToken token ) => await Database.Load( token );
	}
}
