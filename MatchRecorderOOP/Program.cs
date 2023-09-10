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
	case RecorderType.OBSMergedVideo: host.Services.AddSingleton<BaseRecorder , OBSLocalRecorder>(); break;
	default: host.Services.AddSingleton<BaseRecorder , NoVideoRecorder>(); break;
}

host.Services.AddAsyncInitializer<GameDatabaseInitializer>();
host.Services.AddHostedService<RecorderBackgroundService>();


var app = host.Build();

app.UseWebSockets();

app.Map( "/ws" , async ( context ) =>
{
	if( context.Request.Path == "/ws" )
	{
		if( context.WebSockets.IsWebSocketRequest )
		{
			using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
			await Echo( webSocket );
		}
		else
		{
			context.Response.StatusCode = StatusCodes.Status400BadRequest;
		}
	}
} );
app.MapGet( "/ping" , () => Results.Ok() );
app.MapGet( "/messages" , ( ModMessageQueue queue ) =>
{
	return ReturnQueuedMessages( queue );
} );
app.MapPost( $"/{nameof( EndMatchMessage ).ToLowerInvariant()}" , ( EndMatchMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
app.MapPost( $"/{nameof( EndRoundMessage ).ToLowerInvariant()}" , ( EndRoundMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
app.MapPost( $"/{nameof( StartMatchMessage ).ToLowerInvariant()}" , ( StartMatchMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );
app.MapPost( $"/{nameof( StartRoundMessage ).ToLowerInvariant()}" , ( StartRoundMessage message , ModMessageQueue queue ) => QueueAndReturnOK( message , queue ) );

#if DEBUG
//Debugger.Launch();
#endif

await app.InitAsync();
await app.RunAsync();


static async Task Echo( WebSocket webSocket )
{
	var buffer = new byte [1024 * 4];
	var receiveResult = await webSocket.ReceiveAsync(
		new ArraySegment<byte>( buffer ) , CancellationToken.None );

	while( !receiveResult.CloseStatus.HasValue )
	{
		await webSocket.SendAsync(
			new ArraySegment<byte>( buffer , 0 , receiveResult.Count ) ,
			receiveResult.MessageType ,
			receiveResult.EndOfMessage ,
			CancellationToken.None );

		receiveResult = await webSocket.ReceiveAsync(
			new ArraySegment<byte>( buffer ) , CancellationToken.None );
	}

	await webSocket.CloseAsync(
		receiveResult.CloseStatus.Value ,
		receiveResult.CloseStatusDescription ,
		CancellationToken.None );
}

static IResult QueueAndReturnOK( BaseMessage message , ModMessageQueue queue )
{
	queue.RecorderMessageQueue.Enqueue( message );
	//Results.Ok();
	return ReturnQueuedMessages( queue );
}

static IResult ReturnQueuedMessages( ModMessageQueue queue )
{
	var hudMessages = queue.ClientMessageQueue.ToArray();
	queue.ClientMessageQueue.Clear();
	return Results.Json( hudMessages , contentType: MediaTypeNames.Application.Json );
}