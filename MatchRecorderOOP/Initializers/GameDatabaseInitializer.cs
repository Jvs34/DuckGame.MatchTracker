using Extensions.Hosting.AsyncInitialization;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder.Initializers
{
	public sealed class GameDatabaseInitializer : IAsyncInitializer
	{
		public IGameDatabase Database { get; }

		public GameDatabaseInitializer( IConfiguration config , IGameDatabase db )
		{
			Database = db;
			config.Bind( Database.SharedSettings );
		}

		public async Task InitializeAsync( CancellationToken token ) => await Database.Load();
	}
}
