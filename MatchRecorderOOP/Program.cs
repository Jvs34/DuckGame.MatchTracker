using MatchRecorder;
using MatchRecorder.Initializers;
using MatchRecorderShared;
using MatchRecorderShared.Messages;
using MatchTracker;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

host.Services.AddAsyncInitializer<GameDatabaseInitializer>();
host.Services.AddSingleton<IGameDatabase , LiteDBGameDatabase>();
host.Services.AddSingleton<ModMessageQueue>();
//host.Services.AddHostedService<MatchRecorderService>();

var app = host.Build();

app.MapGet( "/api/ping" , () =>
{
	Results.Ok();
} );

app.MapPost( $"/api/{nameof( EndMatchMessage ).ToLowerInvariant()}" , ( EndMatchMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
app.MapPost( $"/api/{nameof( EndRoundMessage ).ToLowerInvariant()}" , ( EndRoundMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
app.MapPost( $"/api/{nameof( StartMatchMessage ).ToLowerInvariant()}" , ( StartMatchMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
app.MapPost( $"/api/{nameof( StartRoundMessage ).ToLowerInvariant()}" , ( StartRoundMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );

await app.RunAsync();

static IResult QueueAndReturnOK( BaseMessage message , ModMessageQueue queue )
{
	queue.MessageQueue.Enqueue( message );
	return Results.Ok();
}