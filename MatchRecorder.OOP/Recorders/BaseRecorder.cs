using MatchRecorder.Shared.Enums;
using MatchRecorder.Shared.Messages;
using MatchShared.Databases.Interfaces;
using MatchShared.DataClasses;
using MatchShared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatchRecorder.OOP.Recorders;

internal abstract class BaseRecorder
{
	public RecorderType RecorderConfigType { get; protected set; }
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
		ILogger<BaseRecorder> logger,
		IGameDatabase db,
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

	public async Task StartRecordingMatch( StartMatchMessage message )
	{
		if( IsRecordingMatch )
		{
			return;
		}

		IsRecordingMatch = true;

		CurrentMatch = new MatchData
		{
			//TimeStarted = message.TimeStarted , //TODO: differentiate between tracking time and recording time
			Players = message.Players,
			Teams = message.Teams
		};

		await StartRecordingMatchInternal();
		await AddOrUpdateMissingPlayers( message.PlayersData );
	}

	public async Task StopRecordingMatch( EndMatchMessage message = null )
	{
		if( !IsRecordingMatch )
		{
			return;
		}

		IsRecordingMatch = false;

		//CurrentMatch.TimeEnded = message?.TimeEnded ?? DateTime.Now; //TODO: differentiate between tracking time and recording time
		CurrentMatch.Players = message?.Players ?? new();
		CurrentMatch.Teams = message?.Teams ?? new();
		CurrentMatch.Winner = message?.Winner ?? new();

		await StopRecordingMatchInternal();
		await AddOrUpdateMissingPlayers( message?.PlayersData ?? new() );
	}

	public async Task StartRecordingRound( StartRoundMessage message )
	{
		if( IsRecordingRound || !IsRecordingMatch )
		{
			return;
		}

		IsRecordingRound = true;

		CurrentRound = new RoundData
		{
			//TimeStarted = message.TimeStarted , //TODO: differentiate between tracking time and recording time
			LevelName = message.LevelName,
			Players = message.Players,
			Teams = message.Teams
		};

		await StartRecordingRoundInternal();
	}

	public async Task StopRecordingRound( EndRoundMessage message = null )
	{
		if( !IsRecordingRound || !IsRecordingMatch )
		{
			return;
		}

		IsRecordingRound = false;

		//CurrentRound.TimeEnded = message?.TimeEnded ?? DateTime.Now; //TODO: differentiate between tracking time and recording time
		CurrentRound.Players = message?.Players ?? new();
		CurrentRound.Teams = message?.Teams ?? new();
		CurrentRound.Winner = message?.Winner ?? new();

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

	protected Task<MatchData> StartCollectingMatchData( DateTime time )
	{
		CurrentMatch.TimeStarted = time;
		CurrentMatch.Name = GameDatabase.SharedSettings.DateTimeToString( time );

		SendHUDmessage( $"Match {CurrentMatch.Name} Started" );

		return Task.FromResult( CurrentMatch );
	}

	protected async Task<MatchData> StopCollectingMatchData( DateTime time )
	{
		SendHUDmessage( $"Match {CurrentMatch.Name} Ended" );

		CurrentMatch.TimeEnded = time;

		await GameDatabase.SaveData( CurrentMatch );
		await GameDatabase.Add( CurrentMatch );

		MatchData newMatchData = CurrentMatch;

		CurrentMatch = null;
		return newMatchData;
	}

	protected Task<RoundData> StartCollectingRoundData( DateTime startTime )
	{
		CurrentRound.MatchName = CurrentMatch.Name;
		CurrentRound.TimeStarted = startTime;
		CurrentRound.Name = GameDatabase.SharedSettings.DateTimeToString( startTime );

		CurrentMatch.Rounds.Add( CurrentRound.Name );

		SendHUDmessage( $"Round #{CurrentMatch.Rounds.Count} Started" );

		return Task.FromResult( CurrentRound );
	}

	protected async Task<RoundData> StopCollectingRoundData( DateTime endTime )
	{
		SendHUDmessage( $"Round #{CurrentMatch.Rounds.Count} Ended" );

		CurrentRound.TimeEnded = endTime;

		await GameDatabase.SaveData( CurrentRound );
		await GameDatabase.Add( CurrentRound );

		RoundData newRoundData = CurrentRound;

		CurrentRound = null;

		return newRoundData;
	}

	public void SendHUDmessage( string message, TextMessagePosition messagePosition = TextMessagePosition.TopLeft )
	{
		Logger.LogInformation( "Sending to client: {message}", message );
		MessageQueue.PushToClientMessageQueue( new TextMessage()
		{
			Message = message,
			MessagePosition = messagePosition
		} );
	}

	public void TrackKill( TrackKillMessage message )
	{
		if( IsRecordingRound && CurrentRound != null )
		{
			CurrentRound.KillsList.Add( message.KillData );
		}
	}

	public async Task CollectObjectData( CollectObjectDataMessage cod )
	{
		await GameDatabase.Add<LevelData>( cod.ObjectDataList.Select( x => x.DatabaseIndex ) );
		await GameDatabase.SaveData( cod.ObjectDataList );
	}

	public async Task CollectLevelData( CollectLevelDataMessage lvl )
	{
		await GameDatabase.Add<LevelData>( lvl.Levels.Select( x => x.DatabaseIndex ) );
		await GameDatabase.SaveData( lvl.Levels );
	}
}
