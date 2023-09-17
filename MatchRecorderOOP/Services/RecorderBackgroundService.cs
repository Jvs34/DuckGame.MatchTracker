using MatchRecorder.Recorders;
using MatchRecorderShared;
using MatchRecorderShared.Messages;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder.Services
{
	internal sealed class RecorderBackgroundService : BackgroundService
	{
		private BaseRecorder Recorder { get; }
		private RecorderSettings RecorderSettings { get; }
		private IGameDatabase GameDatabase { get; }
		private IHostApplicationLifetime AppLifeTime { get; }
		private ILogger<RecorderBackgroundService> Logger { get; }
		private ModMessageQueue MessageQueue { get; }
		private Process DuckGameProcess { get; }

		public RecorderBackgroundService( ILogger<RecorderBackgroundService> logger ,
			ModMessageQueue messageQueue ,
			IGameDatabase db ,
			IOptions<RecorderSettings> recorderSettings ,
			BaseRecorder recorder ,
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
				try { 
				await CheckMessages();
				await Recorder.Update();
				}
				catch( Exception e )
				{
					Console.WriteLine( e );
				}

				if( DuckGameProcess == null || DuckGameProcess.HasExited )
				{
					break;
				}
				await Task.Delay( TimeSpan.FromMilliseconds( 50 ) , token );
			}

			//wait 5 seconds for stuff to completely be done
			var fiveSecondsSource = new CancellationTokenSource();
			fiveSecondsSource.CancelAfter( TimeSpan.FromSeconds( 5 ) );

			await Recorder.StopRecordingRound();
			await Recorder.StopRecordingMatch();

			while( Recorder.IsRecording && !fiveSecondsSource.Token.IsCancellationRequested )
			{
				await Recorder.Update();
				await Task.Delay( TimeSpan.FromMilliseconds( 50 ) , fiveSecondsSource.Token );
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
			Logger.LogInformation( "Received a {messageType} message " , message.MessageType );

			switch( message )
			{
				case StartMatchMessage smm:
					await Recorder.StartRecordingMatch( smm , smm , smm.PlayersData ); break;
				case EndMatchMessage emm:
					await Recorder.StopRecordingMatch( emm , emm , emm , emm.PlayersData ); break;
				case StartRoundMessage srm:
					await Recorder.StartRecordingRound( srm , srm , srm ); break;
				case EndRoundMessage erm:
					await Recorder.StopRecordingRound( erm , erm , erm ); break;
				case TextMessage txtm:
					Logger.LogInformation( "Received: {message}" , txtm.Message ); break;
				case TrackKillMessage tkm:
					await Recorder.TrackKill( tkm.KillData ); break;
				default:
					break;
			}
		}
	}

}