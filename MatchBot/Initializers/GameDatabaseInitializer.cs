﻿using Extensions.Hosting.AsyncInitialization;
using MatchShared.Databases.Interfaces;
using MatchShared.Databases.Settings;
using Microsoft.Extensions.Options;

namespace MatchBot.Initializers;

public sealed class GameDatabaseInitializer : IAsyncInitializer
{
	public IGameDatabase Database { get; }

	public GameDatabaseInitializer( IOptions<SharedSettings> sharedSettings, IGameDatabase db )
	{
		Database = db;
		Database.SharedSettings = sharedSettings.Value;
	}

	public async Task InitializeAsync( CancellationToken token ) => await Database.Load( token );
}
