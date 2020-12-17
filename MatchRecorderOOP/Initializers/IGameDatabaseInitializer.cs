using Extensions.Hosting.AsyncInitialization;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatchRecorder.Initializers
{
	public class IGameDatabaseInitializer : IAsyncInitializer
	{
		public IGameDatabase Database { get; }

		public IGameDatabaseInitializer( IConfiguration config , IGameDatabase db )
		{
			Database = db;
			config.Bind( Database.SharedSettings );
		}

		public async Task InitializeAsync() => await Database.Load();
	}
}
