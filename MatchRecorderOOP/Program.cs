using MatchRecorder;
using MatchRecorder.Initializers;
using MatchTracker;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;


var host = WebApplication.CreateBuilder( args );

var path = Directory.GetCurrentDirectory();

host.Configuration
	.AddJsonFile( Path.Combine( path , "Settings" , "shared.json" ) )
#if DEBUG
	.AddJsonFile( Path.Combine( path , "Settings" , "shared_debug.json" ) )
#endif
	.AddJsonFile( Path.Combine( path , "Settings" , "bot.json" ) )
	.AddJsonFile( Path.Combine( path , "Settings" , "obs.json" ) )
	.AddJsonFile( Path.Combine( path , "Settings" , "uploader.json" ) )
	.AddCommandLine( args );

var services = host.Services;

services.AddAsyncInitializer<GameDatabaseInitializer>();

//database
services.AddSingleton<IGameDatabase , LiteDBGameDatabase>();

//recorder
services.AddSingleton<IModToRecorderMessageQueue , ModToRecorderMessageQueue>();
services.AddHostedService<MatchRecorderService>();
services.AddHostedService<RecorderToModSenderService>();


var app = host.Build();


await app.RunAsync();
