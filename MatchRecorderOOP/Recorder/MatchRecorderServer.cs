using MatchRecorderShared;
using MatchRecorderShared.Messages;
using MatchTracker;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal class MatchRecorderServer : BackgroundService, IDisposable
	{
		private bool disposedValue;
		private IRecorder RecorderHandler { get; }
		public BotSettings BotSettings { get; } = new BotSettings();
		public OBSSettings OBSSettings { get; } = new OBSSettings();
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
		public IMessageQueue MessageQueue { get; }
		public string SettingsPath { get; }
		private IConfigurationRoot Configuration { get; }
		private Task MessageHandlerTask { get; set; }

		public MatchRecorderServer( IMessageQueue messageQueue )
		{
			MessageQueue = messageQueue;
			SettingsPath = Directory.GetCurrentDirectory();
			GameDatabase = new FileSystemGameDatabase();

			Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( SettingsPath , "Settings" ) )
#if DEBUG
				.AddJsonFile( "shared_debug.json" )
#else
				.AddJsonFile( "shared.json" )
#endif
				.AddJsonFile( "bot.json" )
				.AddJsonFile( "obs.json" )
			.Build();

			Configuration.Bind( GameDatabase.SharedSettings );
			Configuration.Bind( BotSettings );
			Configuration.Bind( OBSSettings );

			RecorderHandler = new ObsLocalRecorder( this );
		}

		protected override async Task ExecuteAsync( CancellationToken token )
		{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Console.WriteLine( $"Started {nameof( MatchRecorderServer.ExecuteAsync )}" );

			Task.Factory.StartNew( async() =>
			{
				while( !token.IsCancellationRequested )
				{
					CheckMessages();
					RecorderHandler?.Update();
					await Task.Delay( 100 , token );
				}

				StopRecordingRound();
				StopRecordingMatch();
			} , token , TaskCreationOptions.LongRunning , TaskScheduler.Default );


			await Task.CompletedTask;
#pragma warning restore CS4014
		}

		internal void CheckMessages()
		{
			while( MessageQueue.ReceiveMessagesQueue.TryDequeue( out var message ) )
			{
				OnReceiveMessage( message );
			}
		}

		public void OnReceiveMessage( BaseMessage message )
		{
			Console.WriteLine( $"Received a message of type {message.GetType()}" );

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
				ShowHUDmessage( $"Recorded {CurrentRound.Name}" );
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
			MessageQueue.SendMessagesQueue.Enqueue( new ShowHUDTextMessage()
			{
				Text = message
			} );
		}

		protected virtual void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					GameDatabase?.Dispose();
				}

				disposedValue = true;
			}
		}

		public override void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}

		#endregion UTILITY
	}

}