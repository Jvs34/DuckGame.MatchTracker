using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using DuckGame;
using OBSWebsocketDotNet;
using System.IO;
using MatchTracker;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace MatchRecorder
{
	public class MatchRecorderHandler
	{
		private IRecorder recorderHandler;
		private MatchData currentMatch;
		private RoundData currentRound;

		public bool IsRecording => recorderHandler.IsRecording;
		public string RoundsFolder { get; }
		public string MatchesFolder { get; }
		public GameDatabase GameDatabase { get; private set; }
		public BotSettings BotSettings { get; }
		public String ModPath { get; }

		public MatchRecorderHandler( String modPath )
		{
			ModPath = modPath;
			GameDatabase = new GameDatabase();
			BotSettings = new BotSettings();
			GameDatabase.LoadGlobalDataDelegate += LoadDatabaseGlobalDataFile;
			GameDatabase.LoadMatchDataDelegate += LoadDatabaseMatchDataFile;
			GameDatabase.LoadRoundDataDelegate += LoadDatabaseRoundDataFile;
			GameDatabase.SaveGlobalDataDelegate += SaveDatabaseGlobalDataFile;
			GameDatabase.SaveMatchDataDelegate += SaveDatabaseMatchDataFile;
			GameDatabase.SaveRoundDataDelegate += SaveDatabaseRoundataFile;

			String sharedSettingsPath = Path.Combine( Path.Combine( modPath , "Settings" ) , "shared.json" );
			String botSettingsPath = Path.Combine( Path.Combine( modPath , "Settings" ) , "bot.json" );
			GameDatabase.sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );
			BotSettings = JsonConvert.DeserializeObject<BotSettings>( File.ReadAllText( botSettingsPath ) );
			RoundsFolder = Path.Combine( GameDatabase.sharedSettings.GetRecordingFolder() , GameDatabase.sharedSettings.roundsFolder );
			MatchesFolder = Path.Combine( GameDatabase.sharedSettings.GetRecordingFolder() , GameDatabase.sharedSettings.matchesFolder );

			if( !Directory.Exists( RoundsFolder ) )
				Directory.CreateDirectory( RoundsFolder );

			if( !Directory.Exists( MatchesFolder ) )
				Directory.CreateDirectory( MatchesFolder );

			if( !File.Exists( GameDatabase.sharedSettings.GetGlobalPath() ) )
			{
				GameDatabase.SaveGlobalData( new MatchTracker.GlobalData() ).Wait();
			}

			recorderHandler = new ObsRecorder( this );
			//recorderHandler = new ReplayRecorder( this );

		}

		#region DATABASEDELEGATES
		private async Task<MatchTracker.GlobalData> LoadDatabaseGlobalDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings )
		{
			await Task.CompletedTask;
			return JsonConvert.DeserializeObject<MatchTracker.GlobalData>( File.ReadAllText( sharedSettings.GetGlobalPath() ) );
		}

		private async Task<MatchData> LoadDatabaseMatchDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , string matchName )
		{
			await Task.CompletedTask;
			return JsonConvert.DeserializeObject<MatchData>( File.ReadAllText( sharedSettings.GetMatchPath( matchName ) ) );
		}

		private async Task<RoundData> LoadDatabaseRoundDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , string roundName )
		{
			await Task.CompletedTask;
			return JsonConvert.DeserializeObject<RoundData>( File.ReadAllText( sharedSettings.GetRoundPath( roundName ) ) );
		}

		private async Task SaveDatabaseGlobalDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , MatchTracker.GlobalData globalData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetGlobalPath() , JsonConvert.SerializeObject( globalData , Formatting.Indented ) );
		}

		private async Task SaveDatabaseMatchDataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , String matchName , MatchData matchData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetMatchPath( matchName ) , JsonConvert.SerializeObject( matchData , Formatting.Indented ) );
		}

		private async Task SaveDatabaseRoundataFile( GameDatabase gameDatabase , SharedSettings sharedSettings , String roundName , RoundData roundData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetRoundPath( roundName ) , JsonConvert.SerializeObject( roundData , Formatting.Indented ) );
		}
		#endregion

		//only record game levels for now since we're kind of tied to the gounvirtual stuff
		public bool IsLevelRecordable( Level level )
		{
			return level is GameLevel;
		}

		private void OnConnected( object sender , EventArgs e )
		{
			HUD.AddCornerMessage( HUDCorner.TopRight , "Connected to OBS!!!" );
		}

		private void OnDisconnected( object sender , EventArgs e )
		{
			HUD.AddCornerMessage( HUDCorner.TopRight , "Disconnected from OBS!!!" );
		}

		public void Update()
		{
			recorderHandler?.Update();
		}

		public void StartRecording()
		{
			recorderHandler?.StopRecording();
		}

		public void StopRecording()
		{
			recorderHandler?.StartRecording();
		}

		public void StartCollectingRoundData( DateTime startTime )
		{
			Level lvl = Level.current;

			currentRound = new RoundData()
			{
				levelName = lvl.level ,
				players = new List<PlayerData>() ,
				timeStarted = startTime ,
				isCustomLevel = false ,
				recordingType = recorderHandler.ResultingRecordingType,
			};

			currentRound.name = GameDatabase.sharedSettings.DateTimeToString( currentRound.timeStarted );

			foreach( Profile pro in Profiles.active )
			{
				currentRound.players.Add( CreatePlayerDataFromProfile( pro ) );
			}

			if( lvl is GameLevel gl )
			{
				currentRound.isCustomLevel = gl.isCustomLevel;
			}

			if( currentMatch != null )
			{
				currentMatch.rounds.Add( GameDatabase.sharedSettings.DateTimeToString( currentRound.timeStarted ) );
			}
		}

		public void StopCollectingRoundData( DateTime endTime )
		{
			if( currentRound == null )
			{
				return;
			}

			Team winner = null;

			if( GameMode.lastWinners.Count > 0 )
			{
				winner = GameMode.lastWinners.First()?.team;
			}

			if( winner != null )
			{
				currentRound.winner = CreateTeamDataFromTeam( winner );
			}

			currentRound.timeEnded = endTime;

			GameDatabase.SaveRoundData( GameDatabase.sharedSettings.DateTimeToString( currentRound.timeStarted ) , currentRound ).Wait();

			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;
			globalData.rounds.Add( currentRound.name );
			GameDatabase.SaveGlobalData( globalData ).Wait();

			currentRound = null;
		}

		public void TryCollectingMatchData()
		{
			//try saving the match if there's one and it's got at least one round
			if( currentMatch != null && currentMatch.rounds.Count > 0 )
			{
				StopCollectingMatchData();
			}

			//try starting to collect match data regardless, it'll only be saved if there's at least one round later on
			StartCollectingMatchData();
		}

		private void StartCollectingMatchData()
		{
			currentMatch = new MatchData
			{
				timeStarted = DateTime.Now ,
				rounds = new List<string>() ,
				players = new List<PlayerData>() ,
			};

			currentMatch.name = GameDatabase.sharedSettings.DateTimeToString( currentMatch.timeStarted );
		}

		private void StopCollectingMatchData()
		{
			if( currentMatch == null )
			{
				return;
			}

			currentMatch.timeEnded = DateTime.Now;
			Team winner = null;

			if( Teams.winning.Count > 0 )
			{
				winner = Teams.winning.First();
			}

			if( winner != null )
			{
				currentMatch.winner = CreateTeamDataFromTeam( winner );
			}

			foreach( Profile pro in Profiles.active )
			{
				currentMatch.players.Add( CreatePlayerDataFromProfile( pro ) );
			}

			GameDatabase.SaveMatchData( GameDatabase.sharedSettings.DateTimeToString( currentMatch.timeStarted ) , currentMatch ).Wait();

			//also add this match to the globaldata as well
			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;
			globalData.matches.Add( currentMatch.name );

			//try adding the players from the matchdata into the globaldata

			foreach( PlayerData ply in currentMatch.players )
			{
				if( !globalData.players.Any( p => p.userId == ply.userId ) )
				{
					ply.team = null;

					globalData.players.Add( ply );
				}
			}

			GameDatabase.SaveGlobalData( globalData ).Wait();

			currentMatch = null;
		}

		private TeamData CreateTeamDataFromTeam( Team team )
		{
			TeamData td = new TeamData()
			{
				hasHat = team.hasHat ,
				score = team.score ,
				hatName = team.name ,
				isCustomHat = team.customData != null ,
			};

			return td;
		}

		private PlayerData CreatePlayerDataFromProfile( Profile profile )
		{
			PlayerData pd = new PlayerData
			{
				userId = profile.steamID.ToString() ,
				name = profile.name ,
				team = CreateTeamDataFromTeam( profile.team )
			};
			//I could've done this with an inlined check but I had other shit to call in here so not yet
			if( !Network.isActive )
			{
				pd.userId = profile.id;
			}

			//search for this profile on the globaldata, if it's there fill in the rest of the info
			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;

			foreach( var ply in globalData.players )
			{
				if( pd.Equals( ply ) )
				{
					pd.discordId = ply.discordId;
					pd.nickName = ply.nickName;
				}
			}

			return pd;
		}
	}

	#region HOOKS
	[HarmonyPatch( typeof( Level ) , nameof( Level.UpdateCurrentLevel ) )]
	internal static class UpdateLoop
	{
		private static void Prefix()
		{

			MatchRecorderMod.Recorder?.Update();
		}
	}

	//start recording
	[HarmonyPatch( typeof( VirtualTransition ) , nameof( VirtualTransition.GoUnVirtual ) )]
	internal static class VirtualTransition_GoUnVirtual
	{
		private static void Postfix()
		{
			//only bother if the current level is something we care about
			if( MatchRecorderMod.Recorder.IsLevelRecordable( Level.current ) )
			{
				MatchRecorderMod.Recorder.StartRecording();
			}
		}
	}

	//save the video and stop recording
	[HarmonyPatch( typeof( Level ) , "set_current" )]
	internal static class Level_SetCurrent
	{
		//changed the Postfix to a Prefix so we can get the Level.current before it's changed to the new one
		//as we use it to check if the nextlevel is going to be a GameLevel if this one is a RockScoreboard, then we try collecting matchdata again
		private static void Prefix( Level value )
		{
			//regardless if the current level can be recorded or not, we're done with the current recording so just save and stop
			if( MatchRecorderMod.Recorder.IsRecording )
			{
				MatchRecorderMod.Recorder.StopRecording();
			}

			//only really useful in multiplayer, since continuing a match from the endgame screen doesn't trigger ResetMatchStuff on other clients
			//so unfortunately we have to do this hack
			if( Network.isActive && Network.isClient && !Level.core.gameInProgress )
			{
				Level oldValue = Level.current;
				Level newValue = value;
				if( oldValue is RockScoreboard && MatchRecorderMod.Recorder.IsLevelRecordable( newValue ) )
				{
					MatchRecorderMod.Recorder?.TryCollectingMatchData();
				}
			}
		}
	}

	//this is called once when a new match starts, but not if you're a client and in multiplayer and the host decides to continue from the endgame screen instead of going
	//back to lobby first
	[HarmonyPatch( typeof( Main ) , "ResetMatchStuff" )]
	internal static class Main_ResetMatchStuff
	{
		private static void Prefix()
		{
			MatchRecorderMod.Recorder?.TryCollectingMatchData();
		}
	}
	#endregion
}
