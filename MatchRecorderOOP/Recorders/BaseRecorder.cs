using MatchRecorder.Services;
using MatchRecorderShared.Messages;
using MatchTracker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchRecorder.Recorders
{
	internal abstract class BaseRecorder
	{
		public virtual bool IsRecording { get; }
		public virtual RecordingType ResultingRecordingType { get; set; }
		public virtual bool IsRecordingRound { get; }
		public bool IsRecordingMatch { get; set; }
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

		protected ILogger<BaseRecorder> Logger { get; set; }
		protected IGameDatabase GameDatabase { get; set; }
		protected ModMessageQueue MessageQueue { get; set; }

		public BaseRecorder(
			ILogger<BaseRecorder> logger ,
			IGameDatabase db ,
			ModMessageQueue messageQueue
			)
		{
			Logger = logger;
			GameDatabase = db;
			MessageQueue = messageQueue;
		}

		private async Task AddOrUpdateMissingPlayers( List<PlayerData> players )
		{
			foreach( var player in players )
			{
				await GameDatabase.Add( player );

				var dbPlayerData = await GameDatabase.GetData<PlayerData>( player.DatabaseIndex );
				
				if( dbPlayerData != null )
				{
					dbPlayerData.Name = player.Name;
					await GameDatabase.SaveData( dbPlayerData );
				}
				else
				{
					await GameDatabase.SaveData( player );
				}
			}
		}

		public async Task StartRecordingMatch( IPlayersList playersList , ITeamsList teamsList , List<PlayerData> players )
		{
			if( IsRecordingMatch )
			{
				return;
			}

			IsRecordingMatch = true;

			PendingMatchData.Players = playersList.Players;
			PendingMatchData.Teams = teamsList.Teams;
			await StartRecordingMatchInternal();
			await AddOrUpdateMissingPlayers( players );
		}

		public async Task StopRecordingMatch( IPlayersList playersList = null , ITeamsList teamsList = null , IWinner winner = null , List<PlayerData> players = null )
		{
			if( !IsRecordingMatch )
			{
				return;
			}

			IsRecordingMatch = false;

			PendingMatchData.Players = playersList.Players ?? new();
			PendingMatchData.Teams = teamsList.Teams ?? new();
			PendingMatchData.Winner = winner.Winner ?? new();
			await StopRecordingMatchInternal();
			await AddOrUpdateMissingPlayers( players );
		}

		public async Task StartRecordingRound( ILevelName levelName , IPlayersList playersList , ITeamsList teamsList )
		{
			PendingRoundData.LevelName = levelName.LevelName;
			PendingRoundData.Players = playersList.Players;
			PendingRoundData.Teams = teamsList.Teams;
			await StartRecordingRoundInternal();
		}

		public async Task StopRecordingRound( IPlayersList playersList = null , ITeamsList teamsList = null , IWinner winner = null )
		{
			if( !IsRecordingRound )
			{
				return;
			}

			PendingRoundData.Players = playersList.Players ?? new();
			PendingRoundData.Teams = teamsList.Teams ?? new();
			PendingRoundData.Winner = winner.Winner ?? new();
			await StopRecordingRoundInternal();
		}

		#region ABSTRACT
		protected abstract Task StartRecordingMatchInternal();
		protected abstract Task StartRecordingRoundInternal();
		protected abstract Task StopRecordingMatchInternal();
		protected abstract Task StopRecordingRoundInternal();
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

			CurrentMatch?.Rounds.Add( GameDatabase.SharedSettings.DateTimeToString( CurrentRound.TimeStarted ) );

			return Task.FromResult( CurrentRound );
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
