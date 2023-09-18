using MatchRecorder.Services;
using MatchRecorderShared;
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
		public bool IsRecordingRound { get; set; }
		public bool IsRecordingMatch { get; set; }
		private MatchData CurrentMatch { get; set; }
		private RoundData CurrentRound { get; set; }
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

			CurrentMatch = new MatchData
			{
				Players = playersList.Players ,
				Teams = teamsList.Teams
			};

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

			CurrentMatch.Players = playersList.Players ?? new();
			CurrentMatch.Teams = teamsList.Teams ?? new();
			CurrentMatch.Winner = winner.Winner ?? new();

			await StopRecordingMatchInternal();
			await AddOrUpdateMissingPlayers( players );
		}

		public async Task StartRecordingRound( ILevelName levelName , IPlayersList playersList , ITeamsList teamsList )
		{
			if( IsRecordingRound || !IsRecordingMatch )
			{
				return;
			}

			IsRecordingRound = true;

			CurrentRound = new RoundData
			{
				LevelName = levelName.LevelName ,
				Players = playersList.Players ,
				Teams = teamsList.Teams
			};

			await StartRecordingRoundInternal();
		}

		public async Task StopRecordingRound( IPlayersList playersList = null , ITeamsList teamsList = null , IWinner winner = null )
		{
			if( !IsRecordingRound || !IsRecordingMatch )
			{
				return;
			}

			IsRecordingRound = false;

			CurrentRound.Players = playersList.Players ?? new();
			CurrentRound.Teams = teamsList.Teams ?? new();
			CurrentRound.Winner = winner.Winner ?? new();

			await StopRecordingRoundInternal();
		}

		#region ABSTRACT


		/// <summary>
		/// Must call <see cref="StartCollectingMatchData"/>
		/// </summary>
		/// <returns></returns>
		protected abstract Task StartRecordingMatchInternal();

		/// <summary>
		/// Must call <see cref="StopCollectingRoundData"/>
		/// </summary>
		/// <returns></returns>
		protected abstract Task StartRecordingRoundInternal();

		/// <summary>
		/// Must call <see cref="StopCollectingMatchData"/>
		/// </summary>
		/// <returns></returns>
		protected abstract Task StopRecordingMatchInternal();

		/// <summary>
		/// Must call <see cref="StopCollectingRoundData"/>
		/// </summary>
		/// <returns></returns>
		protected abstract Task StopRecordingRoundInternal();
		public abstract Task Update();
		#endregion

		internal Task<MatchData> StartCollectingMatchData( DateTime time )
		{
			CurrentMatch.TimeStarted = time;
			CurrentMatch.Name = GameDatabase.SharedSettings.DateTimeToString( time );

			SendHUDmessage( $"Match {CurrentMatch.Name} Started" );

			return Task.FromResult( CurrentMatch );
		}

		internal async Task<MatchData> StopCollectingMatchData( DateTime time )
		{
			SendHUDmessage( $"Match {CurrentMatch.Name} Ended" );

			CurrentMatch.TimeEnded = time;

			await GameDatabase.SaveData( CurrentMatch );
			await GameDatabase.Add( CurrentMatch );

			MatchData newMatchData = CurrentMatch;

			CurrentMatch = null;
			return newMatchData;
		}

		internal Task<RoundData> StartCollectingRoundData( DateTime startTime )
		{
			CurrentRound.MatchName = CurrentMatch.Name;
			CurrentRound.TimeStarted = startTime;
			CurrentRound.Name = GameDatabase.SharedSettings.DateTimeToString( startTime );

			CurrentMatch.Rounds.Add( CurrentRound.Name );

			SendHUDmessage( $"Round #{CurrentMatch.Rounds.Count} Started" );

			return Task.FromResult( CurrentRound );
		}

		internal async Task<RoundData> StopCollectingRoundData( DateTime endTime )
		{
			SendHUDmessage( $"Round #{CurrentMatch.Rounds.Count} Ended" );

			CurrentRound.TimeEnded = endTime;

			await GameDatabase.SaveData( CurrentRound );
			await GameDatabase.Add( CurrentRound );

			RoundData newRoundData = CurrentRound;

			CurrentRound = null;

			return newRoundData;
		}

		public void SendHUDmessage( string message , TextMessagePosition messagePosition = TextMessagePosition.TopLeft )
		{
			Logger.LogInformation( "Sending to client: {message}" , message );
			MessageQueue.PushToClientMessageQueue( new TextMessage()
			{
				Message = message ,
				MessagePosition = messagePosition
			} );
		}

		public void TrackKill( KillData killData )
		{
			if( IsRecordingRound && CurrentRound != null )
			{
				CurrentRound.KillsList.Add( killData );
			}
		}
	}
}
