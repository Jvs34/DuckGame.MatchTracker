using MatchRecorder;
using MatchRecorder.Initializers;
using MatchRecorder.Recorders;
using MatchRecorder.Services;
using MatchRecorderShared;
using MatchRecorderShared.Messages;
using MatchTracker;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Threading;
using System.Net.WebSockets;

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
host.Services.AddSingleton<IGameDatabase , LiteDBGameDatabase>();
host.Services.AddSingleton<ModMessageQueue>();


switch( host.Configuration.Get<RecorderSettings>().RecorderType )
{
	case RecorderType.OBSMergedVideo: host.Services.AddSingleton<BaseRecorder , OBSMergedVideoRecorder>(); break;
	default: host.Services.AddSingleton<BaseRecorder , NoVideoRecorder>(); break;
}

host.Services.AddAsyncInitializer<GameDatabaseInitializer>();
host.Services.AddHostedService<RecorderBackgroundService>();


var app = host.Build();

app.MapGet( "/messages" , ( ModMessageQueue queue ) =>
{
	return ReturnQueuedMessages( queue );
} );

DefineEndPoint<EndMatchMessage>( app );
DefineEndPoint<EndRoundMessage>( app );
DefineEndPoint<StartMatchMessage>( app );
DefineEndPoint<StartRoundMessage>( app );
DefineEndPoint<TextMessage>( app );
DefineEndPoint<TrackKillMessage>( app );

await app.InitAsync();
await app.RunAsync();

static IResult QueueAndReturnOK( BaseMessage message , ModMessageQueue queue )
{
	queue.RecorderMessageQueue.Enqueue( message );
	return Results.Ok();
}

static void DefineEndPoint<T>( WebApplication app ) where T : BaseMessage
{
	app.MapPost( $"/{typeof( T ).Name.ToLowerInvariant()}" , ( T message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
}

static IResult ReturnQueuedMessages( ModMessageQueue queue )
{
	var hudMessages = queue.ClientMessageQueue.ToArray();
	queue.ClientMessageQueue.Clear();
	return Results.Json( hudMessages , contentType: MediaTypeNames.Application.Json );
}