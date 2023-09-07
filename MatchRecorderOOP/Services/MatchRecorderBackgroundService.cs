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
	internal sealed class MatchRecorderBackgroundService : BackgroundService
	{
		private BaseRecorder Recorder { get; }
		private RecorderSettings RecorderSettings { get; }

		private IGameDatabase GameDatabase { get; }

		private IHostApplicationLifetime AppLifeTime { get; }
		private ILogger<MatchRecorderBackgroundService> Logger { get; }
		private ModMessageQueue MessageQueue { get; }
		private Task MessageHandlerTask { get; set; }
		private Process DuckGameProcess { get; }

		public MatchRecorderBackgroundService( ILogger<MatchRecorderBackgroundService> logger ,
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
			if( !RecorderSettings.RecordingEnabled )
			{
				return;
			}

			Logger.LogInformation( $"Started {nameof( ExecuteAsync )}" );

			var task = Task.Factory.StartNew( async () =>
			{
				while( !token.IsCancellationRequested )
				{
					CheckMessages();
					await Recorder?.Update();
					if( DuckGameProcess == null || DuckGameProcess.HasExited )
					{
						break;
					}
					await Task.Delay( TimeSpan.FromMilliseconds( 100 ) , token );
				}

				//wait 5 seconds for stuff to completely be done
				var fiveSecondsSource = new CancellationTokenSource();
				fiveSecondsSource.CancelAfter( TimeSpan.FromSeconds( 5 ) );

				Recorder?.StopRecordingRound();
				Recorder?.StopRecordingMatch();

				while( Recorder.IsRecording && !fiveSecondsSource.Token.IsCancellationRequested )
				{
					Recorder?.Update();
					await Task.Delay( TimeSpan.FromMilliseconds( 100 ) , fiveSecondsSource.Token );
				}

				//request the app host to close the process
				AppLifeTime.StopApplication();
			} , token , TaskCreationOptions.LongRunning , TaskScheduler.Default );

			await Task.CompletedTask;
		}

		internal void CheckMessages()
		{
			while( MessageQueue.RecorderMessageQueue.TryDequeue( out var message ) )
			{
				OnReceiveMessage( message );
			}
		}

		public void OnReceiveMessage( BaseMessage message )
		{
			Logger.LogInformation( "Received a message of type {messageType}" , message.MessageType );

			switch( message )
			{
				case StartMatchMessage smm:
					{
						Recorder?.StartRecordingMatch( smm , smm );
						break;
					}
				case EndMatchMessage emm:
					{
						//TODO: check if PlayersData exists in the database and add them otherwise
						Recorder?.StopRecordingMatch( emm , emm , emm );
						break;
					}
				case StartRoundMessage srm:
					{
						Recorder?.StartRecordingRound( srm , srm , srm );
						break;
					}
				case EndRoundMessage erm:
					{
						Recorder?.StopRecordingRound( erm , erm , erm );
						break;
					}
				default:
					break;
			}
		}
	}

}