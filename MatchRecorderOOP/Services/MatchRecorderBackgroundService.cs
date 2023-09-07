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
		private IRecorder Recorder { get; }
		private RecorderSettings RecorderSettings { get; }
		private MatchData CurrentMatch { get; set; }
		private RoundData CurrentRound { get; set; }

		/// <summary>
		/// Data that has arrived from network messages yet to be processed
		/// </summary>
		private MatchData PendingMatchData { get; set; } = new MatchData();

		/// <summary>
		/// Data that has arrived from network messages yet to be processed
		/// </summary>
		private RoundData PendingRoundData { get; set; } = new RoundData();

		private IGameDatabase GameDatabase { get; }
		private bool IsRecordingRound => Recorder.IsRecording;
		private bool IsRecordingMatch { get; set; }
		private IHostApplicationLifetime AppLifeTime { get; }
		private ILogger<MatchRecorderBackgroundService> Logger { get; }
		private ModMessageQueue MessageQueue { get; }
		private Task MessageHandlerTask { get; set; }
		private Process DuckGameProcess { get; }

		public MatchRecorderBackgroundService( ILogger<MatchRecorderBackgroundService> logger ,
			ModMessageQueue messageQueue ,
			IGameDatabase db ,
			IOptions<RecorderSettings> recorderSettings ,
			IRecorder recorder ,
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
					Recorder?.Update();
					if( DuckGameProcess == null || DuckGameProcess.HasExited )
					{
						break;
					}
					await Task.Delay( TimeSpan.FromMilliseconds( 100 ) , token );
				}

				//wait 5 seconds for stuff to completely be done
				var fiveSecondsSource = new CancellationTokenSource();
				fiveSecondsSource.CancelAfter( TimeSpan.FromSeconds( 5 ) );

				StopRecordingRound();
				StopRecordingMatch();

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
						if( !IsRecordingMatch )
						{
							PendingMatchData.Players = smm.Players;
							PendingMatchData.Teams = smm.Teams;

							//TODO: check if PlayersData exists in the database and add them otherwise

							IsRecordingMatch = true;
							StartRecordingMatch();
						}
						break;
					}
				case EndMatchMessage emm:
					{
						if( IsRecordingMatch )
						{
							PendingMatchData.Players = emm.Players;
							PendingMatchData.Teams = emm.Teams;
							PendingMatchData.Winner = emm.Winner;

							//TODO: check if PlayersData exists in the database and add them otherwise

							IsRecordingMatch = false;
							StopRecordingMatch();
						}

						break;
					}
				case StartRoundMessage srm:
					{
						PendingRoundData.LevelName = srm.LevelName;
						PendingRoundData.Players = srm.Players;
						PendingRoundData.Teams = srm.Teams;

						StartRecordingRound();
						break;
					}
				case EndRoundMessage erm:
					{
						if( IsRecordingRound )
						{
							PendingRoundData.Players = erm.Players;
							PendingRoundData.Teams = erm.Teams;
							PendingRoundData.Winner = erm.Winner;

							StopRecordingRound();
						}

						break;
					}
				default:
					break;
			}
		}

		private void StartRecordingMatch()
		{
			if( CurrentRound != null )
			{
				SendHUDmessage( $"Recording {CurrentRound.Name}" );
			}
			Recorder?.StartRecordingMatch();
		}

		private void StopRecordingMatch()
		{
			if( CurrentMatch != null )
			{
				SendHUDmessage( $"Recorded Match{CurrentMatch.Name}" );
			}

			Recorder?.StopRecordingMatch();
		}

		private void StartRecordingRound()
		{
			Recorder?.StartRecordingRound();
		}

		private void StopRecordingRound()
		{
			if( CurrentRound != null )
			{
				SendHUDmessage( $"Recorded {CurrentRound.Name}" );
			}

			Recorder?.StopRecordingRound();
		}

		#region UTILITY

		public void SendHUDmessage( string message )
		{
			Logger.LogInformation( "" );
			MessageQueue.ClientMessageQueue.Enqueue( new ClientHUDMessage()
			{
				Message = message
			} );
		}

		#endregion UTILITY
	}

}