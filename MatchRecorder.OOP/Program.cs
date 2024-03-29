﻿using MatchRecorder.OOP;
using MatchRecorder.OOP.Initializers;
using MatchRecorder.OOP.Recorders;
using MatchRecorder.OOP.Services;
using MatchRecorder.Shared.Enums;
using MatchRecorder.Shared.Messages;
using MatchRecorder.Shared.Settings;
using MatchShared.Databases;
using MatchShared.Databases.Interfaces;
using MatchShared.Databases.LiteDB;
using MatchShared.Databases.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Net.Mime;

var host = WebApplication.CreateBuilder( args );

host.Configuration
	.AddJsonFile( Path.Combine( "Settings", "shared.json" ) )
#if DEBUG
	.AddJsonFile( Path.Combine( "Settings", "shared_debug.json" ), true )
#endif
	.AddJsonFile( Path.Combine( "Settings", "obs.json" ) )
	.AddCommandLine( args )
	.AddEnvironmentVariables();

host.Services.AddOptions<SharedSettings>().BindConfiguration( string.Empty );
host.Services.AddOptions<OBSSettings>().BindConfiguration( string.Empty );
host.Services.AddOptions<RecorderSettings>().BindConfiguration( string.Empty );
host.Services.AddSingleton<IGameDatabase, LiteDBGameDatabase>();
host.Services.AddSingleton<ModMessageQueue>();

host.Services.ConfigureHttpJsonOptions( options => options.SerializerOptions.IncludeFields = true );


switch( host.Configuration.Get<RecorderSettings>().RecorderType )
{
	case RecorderType.OBSRawVideo: host.Services.AddSingleton<BaseRecorder, OBSRawVideoRecorder>(); break;
	//TODO: case RecorderType.OBSLiveStream: host.Services.AddSingleton<BaseRecorder, OBSLiveStreamRecorder>(); break;
	case RecorderType.NoVideo:
	default: host.Services.AddSingleton<BaseRecorder, NoVideoRecorder>(); break;
}

host.Services.AddAsyncInitializer<GameDatabaseInitializer>();
host.Services.AddHostedService<RecorderBackgroundService>();

var app = host.Build();

app.MapGet( "/messages", ( ModMessageQueue queue ) => ReturnQueuedMessages( queue ) );

DefineEndPoint<EndMatchMessage>( app );
DefineEndPoint<EndRoundMessage>( app );
DefineEndPoint<StartMatchMessage>( app );
DefineEndPoint<StartRoundMessage>( app );
DefineEndPoint<TextMessage>( app );
DefineEndPoint<TrackKillMessage>( app );
DefineEndPoint<CollectObjectDataMessage>( app );
DefineEndPoint<CollectLevelDataMessage>( app );
DefineEndPoint<LevelPreviewMessage>( app );
DefineEndPoint<CloseRecorderMessage>( app );

await app.InitAsync();
await app.RunAsync();

static IResult QueueAndReturnOK( BaseMessage message, ModMessageQueue queue )
{
	queue.PushToRecorderQueue( message );
	return Results.Ok();
}

static void DefineEndPoint<T>( WebApplication app ) where T : BaseMessage
{
	app.MapPost( $"/{typeof( T ).Name.ToLowerInvariant()}", ( T message, ModMessageQueue queue ) => QueueAndReturnOK( message, queue ) );
}

static IResult ReturnQueuedMessages( ModMessageQueue queue )
{
	var hudMessages = queue.ClientMessageQueue.ToArray();
	queue.ClientMessageQueue.Clear();
	return Results.Json( hudMessages, contentType: MediaTypeNames.Application.Json );
}