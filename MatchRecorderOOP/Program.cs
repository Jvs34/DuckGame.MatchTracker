using MatchRecorder;
using MatchRecorder.Initializers;
using MatchRecorderShared;
using MatchRecorderShared.Messages;
using MatchTracker;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;

var host = WebApplication.CreateBuilder( args );

host.Configuration
	.AddJsonFile( Path.Combine( "Settings" , "shared.json" ) )
#if DEBUG
	.AddJsonFile( Path.Combine( "Settings" , "shared_debug.json" ) )
#endif
	.AddJsonFile( Path.Combine( "Settings" , "obs.json" ) )
	.AddCommandLine( args );

host.Services.AddOptions<SharedSettings>().BindConfiguration( string.Empty );
host.Services.AddOptions<OBSSettings>().BindConfiguration( string.Empty );
host.Services.AddOptions<RecorderSettings>().BindConfiguration( string.Empty );

host.Services.AddAsyncInitializer<GameDatabaseInitializer>();
host.Services.AddSingleton<IGameDatabase , LiteDBGameDatabase>();
host.Services.AddSingleton<ModMessageQueue>();
host.Services.AddHostedService<MatchRecorderService>();

var app = host.Build();

app.MapGet( "/ping" , () =>
{
	Results.Ok();
} );

app.MapPost( $"/{nameof( EndMatchMessage ).ToLowerInvariant()}" , ( EndMatchMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
app.MapPost( $"/{nameof( EndRoundMessage ).ToLowerInvariant()}" , ( EndRoundMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
app.MapPost( $"/{nameof( StartMatchMessage ).ToLowerInvariant()}" , ( StartMatchMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
app.MapPost( $"/{nameof( StartRoundMessage ).ToLowerInvariant()}" , ( StartRoundMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );

await app.InitAsync();

await app.RunAsync();

static IResult QueueAndReturnOK( BaseMessage message , ModMessageQueue queue )
{
	queue.MessageQueue.Enqueue( message );
	return Results.Ok();
}