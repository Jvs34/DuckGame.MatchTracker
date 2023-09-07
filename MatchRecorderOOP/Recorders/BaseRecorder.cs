using MatchRecorder.Services;
using MatchRecorderShared.Messages;
using MatchTracker;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MatchRecorder.Recorders
{
	internal abstract class BaseRecorder : IRecorder
	{
		public abstract bool IsRecording { get; }
		public abstract RecordingType ResultingRecordingType { get; set; }
		public MatchData CurrentMatch { get; set; }
		public RoundData CurrentRound { get; set; }

		/// <summary>
		/// Data that has arrived from network messages yet to be processed
		/// </summary>
		protected MatchData PendingMatchData { get; set; } = new MatchData();

		/// <summary>
		/// Data that has arrived from network messages yet to be processed
		/// </summary>
		protected RoundData PendingRoundData { get; set; } = new RoundData();

		protected ILogger<IRecorder> Logger { get; set; }
		protected IGameDatabase GameDatabase { get; set; }
		protected ModMessageQueue MessageQueue { get; set; }

		public BaseRecorder(
			ILogger<IRecorder> logger ,
			IGameDatabase db ,
			ModMessageQueue messageQueue
			)
		{
			Logger = logger;
			GameDatabase = db;
			MessageQueue = messageQueue;
		}



		#region ABSTRACT
		public abstract Task StartRecordingMatch();
		public abstract Task StartRecordingRound();
		public abstract Task StopRecordingMatch();
		public abstract Task StopRecordingRound();
		public abstract Task Update();
		#endregion

		internal Task<MatchData> StartCollectingMatchData( DateTime time )
		{
			CurrentMatch = new MatchData
			{
				TimeStarted = time ,
				Name = GameDatabase.SharedSettings.DateTimeToString( time ) ,
			};

			return Task.FromResult( CurrentMatch );
		}

		internal async Task<MatchData> StopCollectingMatchData( DateTime time )
		{
			if( CurrentMatch == null )
			{
				return null;
			}

			CurrentMatch.TimeEnded = time;
			CurrentMatch.Players = PendingMatchData.Players;
			CurrentMatch.Teams = PendingMatchData.Teams;
			CurrentMatch.Winner = PendingMatchData.Winner;

			await GameDatabase.SaveData( CurrentMatch );
			await GameDatabase.Add( CurrentMatch );

			MatchData newMatchData = CurrentMatch;

			CurrentMatch = null;
			PendingMatchData = new MatchData();
			return newMatchData;
		}

		internal Task<RoundData> StartCollectingRoundData( DateTime startTime )
		{
			CurrentRound = new RoundData()
			{
				MatchName = CurrentMatch?.Name ,
				LevelName = PendingRoundData.LevelName ,
				TimeStarted = startTime ,
				Name = GameDatabase.SharedSettings.DateTimeToString( startTime ) ,
				RecordingType = ResultingRecordingType ,
				Players = PendingRoundData.Players ,
				Teams = PendingRoundData.Teams ,
			};

			if( CurrentMatch != null )
			{
				CurrentMatch.Rounds.Add( GameDatabase.SharedSettings.DateTimeToString( CurrentRound.TimeStarted ) );
			}

			return Task.FromResult(CurrentRound);
		}

		internal async Task<RoundData> StopCollectingRoundData( DateTime endTime )
		{
			if( CurrentRound == null )
			{
				return null;
			}

			CurrentRound.Players = PendingRoundData.Players;
			CurrentRound.Teams = PendingRoundData.Teams;
			CurrentRound.Winner = PendingRoundData.Winner;
			CurrentRound.TimeEnded = endTime;

			await GameDatabase.SaveData( CurrentRound );
			await GameDatabase.Add( CurrentRound );

			RoundData newRoundData = CurrentRound;

			CurrentRound = null;
			PendingRoundData = new RoundData();

			return newRoundData;
		}

		public void SendHUDmessage( string message )
		{
			Logger.LogInformation( "Sending to client: {message}" , message );
			MessageQueue.ClientMessageQueue.Enqueue( new ClientHUDMessage()
			{
				Message = message
			} );
		}
	}
}
