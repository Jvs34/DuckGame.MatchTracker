using MatchRecorderShared;
using MatchRecorderShared.Messages;
using MatchTracker;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal class MatchRecorderService : BackgroundService
	{
		private IRecorder RecorderHandler { get; }
		public OBSSettings OBSSettings { get; } = new OBSSettings();
		public RecorderSettings RecorderSettings { get; } = new RecorderSettings();
		public MatchData CurrentMatch { get; private set; }
		public RoundData CurrentRound { get; private set; }

		/// <summary>
		/// Data that has arrived from network messages yet to be processed
		/// </summary>
		private MatchData PendingMatchData { get; set; } = new MatchData();

		/// <summary>
		/// Data that has arrived from network messages yet to be processed
		/// </summary>
		private RoundData PendingRoundData { get; set; } = new RoundData();
		public IGameDatabase GameDatabase { get; }
		public bool IsRecordingRound => RecorderHandler.IsRecording;
		public bool IsRecordingMatch { get; set; }
		public IHostApplicationLifetime AppLifeTime { get; }
		private ILogger<MatchRecorderService> Logger { get; }
		public ModMessageQueue MessageQueue { get; }
		private IConfiguration Configuration { get; }
		private Task MessageHandlerTask { get; set; }
		private Process DuckGameProcess { get; }

		public MatchRecorderService( ILogger<MatchRecorderService> logger , ModMessageQueue messageQueue , IGameDatabase db , IConfiguration configuration , IHostApplicationLifetime lifetime )
		{
			AppLifeTime = lifetime;
			Logger = logger;
			MessageQueue = messageQueue;
			GameDatabase = db;

			Configuration = configuration;

			Configuration.Bind( OBSSettings );
			Configuration.Bind( RecorderSettings );

			RecorderHandler = new ObsLocalRecorder( this );

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

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Logger.LogInformation( $"Started {nameof( MatchRecorderService.ExecuteAsync )}" );

			Task.Factory.StartNew( async () =>
			{
				while( !token.IsCancellationRequested )
				{
					CheckMessages();
					RecorderHandler?.Update();
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

				while( RecorderHandler.IsRecording && !fiveSecondsSource.Token.IsCancellationRequested )
				{
					RecorderHandler?.Update();
					await Task.Delay( TimeSpan.FromMilliseconds( 100 ) , fiveSecondsSource.Token );
				}

				//request the app host to close the process
				AppLifeTime.StopApplication();
			} , token , TaskCreationOptions.LongRunning , TaskScheduler.Default );

			await Task.CompletedTask;
#pragma warning restore CS4014
		}

		internal void CheckMessages()
		{
			while( MessageQueue.MessageQueue.TryDequeue( out var message ) )
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
				ShowHUDmessage( $"Recording {CurrentRound.Name}" );
			}
			RecorderHandler?.StartRecordingMatch();
		}

		private void StopRecordingMatch()
		{
			if( CurrentMatch != null )
			{
				ShowHUDmessage( $"Recorded Match{CurrentMatch.Name}" );
			}

			RecorderHandler?.StopRecordingMatch();
		}

		private void StartRecordingRound()
		{
			RecorderHandler?.StartRecordingRound();
		}

		private void StopRecordingRound()
		{
			if( CurrentRound != null )
			{
				ShowHUDmessage( $"Recorded {CurrentRound.Name}" );
			}

			RecorderHandler?.StopRecordingRound();
		}

		internal MatchData StartCollectingMatchData( DateTime time )
		{
			CurrentMatch = new MatchData
			{
				TimeStarted = time ,
				Name = GameDatabase.SharedSettings.DateTimeToString( time ) ,
			};

			return CurrentMatch;
		}

		internal MatchData StopCollectingMatchData( DateTime time )
		{
			if( CurrentMatch == null )
			{
				return null;
			}

			CurrentMatch.TimeEnded = time;
			CurrentMatch.Players = PendingMatchData.Players;
			CurrentMatch.Teams = PendingMatchData.Teams;
			CurrentMatch.Winner = PendingMatchData.Winner;

			GameDatabase.SaveData( CurrentMatch ).Wait();
			GameDatabase.Add( CurrentMatch ).Wait();

			MatchData newMatchData = CurrentMatch;

			CurrentMatch = null;
			PendingMatchData = new MatchData();
			return newMatchData;
		}

		internal RoundData StartCollectingRoundData( DateTime startTime )
		{
			CurrentRound = new RoundData()
			{
				MatchName = CurrentMatch?.Name ,
				LevelName = PendingRoundData.LevelName ,
				TimeStarted = startTime ,
				Name = GameDatabase.SharedSettings.DateTimeToString( startTime ) ,
				RecordingType = RecorderHandler.ResultingRecordingType ,
				Players = PendingRoundData.Players ,
				Teams = PendingRoundData.Teams ,
			};

			if( CurrentMatch != null )
			{
				CurrentMatch.Rounds.Add( GameDatabase.SharedSettings.DateTimeToString( CurrentRound.TimeStarted ) );
			}

			return CurrentRound;
		}

		internal RoundData StopCollectingRoundData( DateTime endTime )
		{
			if( CurrentRound == null )
			{
				return null;
			}

			CurrentRound.Players = PendingRoundData.Players;
			CurrentRound.Teams = PendingRoundData.Teams;
			CurrentRound.Winner = PendingRoundData.Winner;
			CurrentRound.TimeEnded = endTime;
			GameDatabase.SaveData( CurrentRound ).Wait();
			GameDatabase.Add( CurrentRound ).Wait();

			RoundData newRoundData = CurrentRound;

			CurrentRound = null;
			PendingRoundData = new RoundData();

			return newRoundData;
		}


		#region UTILITY

		public void ShowHUDmessage( string message )
		{
			Logger.LogInformation( "" );
		}

		#endregion UTILITY
	}

}