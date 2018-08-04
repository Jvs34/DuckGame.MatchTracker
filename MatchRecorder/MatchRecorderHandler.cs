using DuckGame;
using Harmony;
using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MatchRecorder
{
	public class MatchRecorderHandler
	{
		private IRecorder recorderHandler;
		public BotSettings BotSettings { get; }
		public MatchData CurrentMatch { get; private set; }
		public RoundData CurrentRound { get; private set; }
		public IGameDatabase GameDatabase { get; private set; }
		public bool IsRecording => recorderHandler.IsRecording;
		public string MatchesFolder { get; }
		public String ModPath { get; }
		public string RoundsFolder { get; }
		private IConfigurationRoot Configuration { get; }

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

			Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( modPath , "Settings" ) )
#if DEBUG
				.AddJsonFile( "shared_debug.json" )
#else
				.AddJsonFile( "shared.json" )
#endif
				.AddJsonFile( "bot.json" )
			.Build();

			Configuration.Bind( GameDatabase.SharedSettings );
			Configuration.Bind( BotSettings );

			RoundsFolder = Path.Combine( GameDatabase.SharedSettings.GetRecordingFolder() , GameDatabase.SharedSettings.roundsFolder );
			MatchesFolder = Path.Combine( GameDatabase.SharedSettings.GetRecordingFolder() , GameDatabase.SharedSettings.matchesFolder );

			if( !Directory.Exists( RoundsFolder ) )
				Directory.CreateDirectory( RoundsFolder );

			if( !Directory.Exists( MatchesFolder ) )
				Directory.CreateDirectory( MatchesFolder );

			if( !File.Exists( GameDatabase.SharedSettings.GetGlobalPath() ) )
			{
				GameDatabase.SaveGlobalData( new MatchTracker.GlobalData() ).Wait();
			}

#if DEBUG
			recorderHandler = new ReplayRecorder( this );
#else
			recorderHandler = new ObsRecorder( this );
#endif
		}

		#region DATABASEDELEGATES

		private async Task<MatchTracker.GlobalData> LoadDatabaseGlobalDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings )
		{
			await Task.CompletedTask;
			return JsonConvert.DeserializeObject<MatchTracker.GlobalData>( File.ReadAllText( sharedSettings.GetGlobalPath() ) );
		}

		private async Task<MatchData> LoadDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string matchName )
		{
			await Task.CompletedTask;
			return JsonConvert.DeserializeObject<MatchData>( File.ReadAllText( sharedSettings.GetMatchPath( matchName ) ) );
		}

		private async Task<RoundData> LoadDatabaseRoundDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string roundName )
		{
			await Task.CompletedTask;
			return JsonConvert.DeserializeObject<RoundData>( File.ReadAllText( sharedSettings.GetRoundPath( roundName ) ) );
		}

		private async Task SaveDatabaseGlobalDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , MatchTracker.GlobalData globalData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetGlobalPath() , JsonConvert.SerializeObject( globalData , Formatting.Indented ) );
		}

		private async Task SaveDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , String matchName , MatchData matchData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetMatchPath( matchName ) , JsonConvert.SerializeObject( matchData , Formatting.Indented ) );
		}

		private async Task SaveDatabaseRoundataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , String roundName , RoundData roundData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetRoundPath( roundName ) , JsonConvert.SerializeObject( roundData , Formatting.Indented ) );
		}

		#endregion DATABASEDELEGATES

		//only record game levels for now since we're kind of tied to the gounvirtual stuff
		public bool IsLevelRecordable( Level level )
		{
			return level is GameLevel;
		}

		public RoundData StartCollectingRoundData( DateTime startTime )
		{
			Level lvl = Level.current;

			CurrentRound = new RoundData()
			{
				levelName = lvl.level ,
				players = new List<PlayerData>() ,
				timeStarted = startTime ,
				isCustomLevel = false ,
				recordingType = recorderHandler.ResultingRecordingType ,
			};

			CurrentRound.name = GameDatabase.SharedSettings.DateTimeToString( CurrentRound.timeStarted );

			foreach( Profile pro in Profiles.active )
			{
				CurrentRound.players.Add( CreatePlayerDataFromProfile( pro ) );
			}

			if( lvl is GameLevel gl )
			{
				CurrentRound.isCustomLevel = gl.isCustomLevel;
			}

			if( CurrentMatch != null )
			{
				CurrentMatch.rounds.Add( GameDatabase.SharedSettings.DateTimeToString( CurrentRound.timeStarted ) );
			}

			return CurrentRound;
		}

		public void StartRecording()
		{
			recorderHandler?.StartRecording();
		}

		public RoundData StopCollectingRoundData( DateTime endTime )
		{
			if( CurrentRound == null )
			{
				return null;
			}

			Team winner = null;

			if( GameMode.lastWinners.Count > 0 )
			{
				winner = GameMode.lastWinners.First()?.team;
			}

			if( winner != null )
			{
				CurrentRound.winner = CreateTeamDataFromTeam( winner );
			}

			CurrentRound.timeEnded = endTime;

			GameDatabase.SaveRoundData( GameDatabase.SharedSettings.DateTimeToString( CurrentRound.timeStarted ) , CurrentRound ).Wait();

			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;
			globalData.rounds.Add( CurrentRound.name );
			GameDatabase.SaveGlobalData( globalData ).Wait();

			RoundData newRoundData = CurrentRound;

			CurrentRound = null;

			return newRoundData;
		}

		public void StopRecording()
		{
			recorderHandler?.StopRecording();
		}

		public void TryCollectingMatchData()
		{
			//try saving the match if there's one and it's got at least one round
			if( CurrentMatch != null && CurrentMatch.rounds.Count > 0 )
			{
				StopCollectingMatchData();
			}

			//try starting to collect match data regardless, it'll only be saved if there's at least one round later on
			StartCollectingMatchData();
		}

		public void Update()
		{
			recorderHandler?.Update();
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

		private MatchData StartCollectingMatchData()
		{
			CurrentMatch = new MatchData
			{
				timeStarted = DateTime.Now ,
				rounds = new List<string>() ,
				players = new List<PlayerData>() ,
			};

			CurrentMatch.name = GameDatabase.SharedSettings.DateTimeToString( CurrentMatch.timeStarted );

			return CurrentMatch;
		}

		private MatchData StopCollectingMatchData()
		{
			if( CurrentMatch == null )
			{
				return null;
			}

			CurrentMatch.timeEnded = DateTime.Now;
			Team winner = null;

			if( Teams.winning.Count > 0 )
			{
				winner = Teams.winning.First();
			}

			if( winner != null )
			{
				CurrentMatch.winner = CreateTeamDataFromTeam( winner );
			}

			foreach( Profile pro in Profiles.active )
			{
				CurrentMatch.players.Add( CreatePlayerDataFromProfile( pro ) );
			}

			GameDatabase.SaveMatchData( GameDatabase.SharedSettings.DateTimeToString( CurrentMatch.timeStarted ) , CurrentMatch ).Wait();

			//also add this match to the globaldata as well
			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;
			globalData.matches.Add( CurrentMatch.name );

			//try adding the players from the matchdata into the globaldata

			foreach( PlayerData ply in CurrentMatch.players )
			{
				if( !globalData.players.Any( p => p.userId == ply.userId ) )
				{
					ply.team = null;

					globalData.players.Add( ply );
				}
			}

			GameDatabase.SaveGlobalData( globalData ).Wait();

			MatchData newMatchData = CurrentMatch;

			CurrentMatch = null;

			return newMatchData;
		}
	}

	#region HOOKS

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

	#endregion HOOKS
}