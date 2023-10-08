using MatchBot.Initializers;
using MatchBot.Services;
using MatchShared.Databases;
using MatchShared.Databases.Interfaces;
using MatchShared.Databases.Settings;


var host = Host.CreateApplicationBuilder( args );

host.Configuration
	.AddJsonFile( Path.Combine( "Settings", "shared.json" ) )
	.AddJsonFile( Path.Combine( "Settings", "bot.json" ) )
	.AddCommandLine( args )
	.AddEnvironmentVariables();

host.Services.AddOptions<SharedSettings>().BindConfiguration( string.Empty );
host.Services.AddOptions<BotSettings>().BindConfiguration( string.Empty );
host.Services.AddSingleton<IGameDatabase, LiteDBGameDatabase>();

host.Services.AddAsyncInitializer<GameDatabaseInitializer>();
host.Services.AddHostedService<DiscordService>();

var app = host.Build();

await app.InitAsync();
await app.RunAsync();