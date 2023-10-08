using MatchRecorder.OOP.Recorders;
using MatchRecorder.Shared.Messages;
using MatchRecorder.Shared.Settings;
using MatchShared.Databases.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder.OOP.Services;

internal sealed class RecorderBackgroundService : BackgroundService
{
	private BaseRecorder Recorder { get; }
	private RecorderSettings RecorderSettings { get; }
	private IGameDatabase GameDatabase { get; }
	private IHostApplicationLifetime AppLifeTime { get; }
	private ILogger<RecorderBackgroundService> Logger { get; }
	private ModMessageQueue MessageQueue { get; }
	private Process DuckGameProcess { get; }

	public RecorderBackgroundService( ILogger<RecorderBackgroundService> logger,
		ModMessageQueue messageQueue,
		IGameDatabase db,
		IOptions<RecorderSettings> recorderSettings,
		BaseRecorder recorder,
		IHostApplicationLifetime lifetime )
	{
		AppLifeTime = lifetime;
		Logger = logger;
		MessageQueue = messageQueue;
		GameDatabase = db;
		RecorderSettings = recorderSettings.Value;
		Recorder = recorder;

		if( RecorderSettings.DuckGameProcessID > 0 )
		{
			DuckGameProcess = Process.GetProcessById( RecorderSettings.DuckGameProcessID );
		}
	}

	protected override async Task ExecuteAsync( CancellationToken token )
	{
		Logger.LogInformation( $"Started {nameof( RecorderBackgroundService )}" );

		while( !token.IsCancellationRequested )
		{
			try
			{
				await CheckMessages();
				await Recorder.Update();
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			if( RecorderSettings.AutoCloseWhenParentDies && ( DuckGameProcess == null || DuckGameProcess.HasExited ) )
			{
				break;
			}

			await Task.Delay( TimeSpan.FromMilliseconds( 50 ), token );
		}

		//wait some time for stuff to completely be done
		var timedSource = new CancellationTokenSource();
		timedSource.CancelAfter( TimeSpan.FromSeconds( 10 ) );

		await Recorder.StopRecordingRound();
		await Recorder.StopRecordingMatch();

		while( Recorder.IsRecording && !timedSource.Token.IsCancellationRequested )
		{
			await Recorder.Update();
			await Task.Delay( TimeSpan.FromMilliseconds( 50 ), timedSource.Token );
		}

		//request the app host to close the process
		AppLifeTime.StopApplication();
	}

	internal async Task CheckMessages()
	{
		while( MessageQueue.RecorderMessageQueue.TryDequeue( out var message ) )
		{
			await OnReceiveMessage( message );
		}
	}

	public async Task OnReceiveMessage( BaseMessage message )
	{
		Logger.LogInformation( "Received a {messageType} message ", message.MessageType );

		switch( message )
		{
			case StartMatchMessage smm: await Recorder.StartRecordingMatch( smm ); break;
			case EndMatchMessage emm: await Recorder.StopRecordingMatch( emm ); break;
			case StartRoundMessage srm: await Recorder.StartRecordingRound( srm ); break;
			case EndRoundMessage erm: await Recorder.StopRecordingRound( erm ); break;
			case TextMessage txtm: Logger.LogInformation( "Received: {message}", txtm ); break;
			case TrackKillMessage tkm: Recorder.TrackKill( tkm ); break;
			case CollectObjectDataMessage cod: await Recorder.CollectObjectData( cod ); break;
			case CloseRecorderMessage: AppLifeTime.StopApplication(); break;
			default:
			break;
		}
	}
}