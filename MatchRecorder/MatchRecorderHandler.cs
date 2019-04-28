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
		public string ModPath { get; }
		public string RoundsFolder { get; }
		private IConfigurationRoot Configuration { get; }
		private JsonSerializerSettings JsonSettings { get; }

		public MatchRecorderHandler( string modPath )
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

			JsonSettings = new JsonSerializerSettings()
			{
				PreserveReferencesHandling = PreserveReferencesHandling.Objects ,
			};

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

			RoundsFolder = Path.Combine( GameDatabase.SharedSettings.GetRecordingFolder() , GameDatabase.SharedSettings.RoundsFolder );
			MatchesFolder = Path.Combine( GameDatabase.SharedSettings.GetRecordingFolder() , GameDatabase.SharedSettings.MatchesFolder );

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
			return JsonConvert.DeserializeObject<MatchTracker.GlobalData>( File.ReadAllText( sharedSettings.GetGlobalPath() ) , JsonSettings );
		}

		private async Task<MatchData> LoadDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string matchName )
		{
			await Task.CompletedTask;
			return JsonConvert.DeserializeObject<MatchData>( File.ReadAllText( sharedSettings.GetMatchPath( matchName ) ) , JsonSettings );
		}

		private async Task<RoundData> LoadDatabaseRoundDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string roundName )
		{
			await Task.CompletedTask;
			return JsonConvert.DeserializeObject<RoundData>( File.ReadAllText( sharedSettings.GetRoundPath( roundName ) ) , JsonSettings );
		}

		private async Task SaveDatabaseGlobalDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , MatchTracker.GlobalData globalData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetGlobalPath() , JsonConvert.SerializeObject( globalData , Formatting.Indented , JsonSettings ) );
		}

		private async Task SaveDatabaseMatchDataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string matchName , MatchData matchData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetMatchPath( matchName ) , JsonConvert.SerializeObject( matchData , Formatting.Indented , JsonSettings ) );
		}

		private async Task SaveDatabaseRoundataFile( IGameDatabase gameDatabase , SharedSettings sharedSettings , string roundName , RoundData roundData )
		{
			await Task.CompletedTask;
			File.WriteAllText( sharedSettings.GetRoundPath( roundName ) , JsonConvert.SerializeObject( roundData , Formatting.Indented , JsonSettings ) );
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
				MatchName = CurrentMatch?.Name,
				LevelName = lvl.level ,
				Players = new List<PlayerData>() ,
				TimeStarted = startTime ,
				IsCustomLevel = false ,
				RecordingType = recorderHandler.ResultingRecordingType ,
			};

			CurrentRound.Name = GameDatabase.SharedSettings.DateTimeToString( CurrentRound.TimeStarted );

			if( lvl is GameLevel gl )
			{
				CurrentRound.IsCustomLevel = gl.isCustomLevel;
			}

			if( CurrentMatch != null )
			{
				CurrentMatch.Rounds.Add( GameDatabase.SharedSettings.DateTimeToString( CurrentRound.TimeStarted ) );
			}

			return CurrentRound;
		}

		public void StartRecording()
		{
			recorderHandler?.StartRecording();
		}

		public void AddTeamAndPlayerData( IWinner winnerObject )
		{
			foreach( Team team in Teams.active )
			{
				winnerObject.Teams.Add( CreateTeamDataFromTeam( team , winnerObject ) );
			}

			foreach( Profile pro in Profiles.active )
			{
				PlayerData ply = CreatePlayerDataFromProfile( pro , winnerObject );
				winnerObject.Players.Add( ply );
			}

			foreach( TeamData teamData in winnerObject.Teams )
			{
				Team team = Teams.active.Find( x => x.name == teamData.hatName );
				if( team != null )
				{
					foreach( Profile pro in team.activeProfiles )
					{
						teamData.Players.Add( CreatePlayerDataFromProfile( pro , winnerObject ) );
					}
				}
			}
		}

		public RoundData StopCollectingRoundData( DateTime endTime )
		{
			if( CurrentRound == null )
			{
				return null;
			}

			AddTeamAndPlayerData( CurrentRound );

			Team winner = null;

			if( GameMode.lastWinners.Count > 0 )
			{
				winner = GameMode.lastWinners.First()?.team;
			}

			if( winner != null )
			{
				CurrentRound.Winner = CreateTeamDataFromTeam( winner , CurrentRound );
			}

			CurrentRound.TimeEnded = endTime;

			GameDatabase.SaveRoundData( GameDatabase.SharedSettings.DateTimeToString( CurrentRound.TimeStarted ) , CurrentRound ).Wait();

			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;
			globalData.Rounds.Add( CurrentRound.Name );
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
			if( CurrentMatch != null && CurrentMatch.Rounds.Count > 0 )
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

		private PlayerData CreatePlayerDataFromProfile( Profile profile , IWinner winnerObject )
		{
			string userId = Network.isActive ? profile.steamID.ToString() : profile.id;

			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;

			PlayerData pd = globalData.Players.Find( x => x.UserId == userId );

			if( pd == null )
			{
				pd = new PlayerData
				{
					UserId = userId ,
					Name = profile.name ,
				};

				//search for this profile on the globaldata, if it's there fill in the rest of the info
				foreach( var ply in globalData.Players )
				{
					if( pd.Equals( ply ) )
					{
						pd.DiscordId = ply.DiscordId;
						pd.NickName = ply.NickName;
						break;
					}
				}
			}

			return pd;
		}

		private TeamData CreateTeamDataFromTeam( Team team , IWinner winnerObject )
		{
			//try to find a teamobject that's already there
			TeamData td = null;

			if( winnerObject != null )
			{
				td = winnerObject.Teams.Find( x => x.hatName == team.name );
			}

			if( td == null )
			{
				td = new TeamData()
				{
					hasHat = team.hasHat ,
					score = team.score ,
					hatName = team.name ,
					isCustomHat = team.customData != null ,
				};
			}

			return td;
		}

		private MatchData StartCollectingMatchData()
		{
			CurrentMatch = new MatchData
			{
				TimeStarted = DateTime.Now ,
			};

			CurrentMatch.Name = GameDatabase.SharedSettings.DateTimeToString( CurrentMatch.TimeStarted );

			return CurrentMatch;
		}

		private MatchData StopCollectingMatchData()
		{
			if( CurrentMatch == null )
			{
				return null;
			}

			CurrentMatch.TimeEnded = DateTime.Now;

			AddTeamAndPlayerData( CurrentMatch );

			Team winner = null;

			if( Teams.winning.Count > 0 )
			{
				winner = Teams.winning.First();
			}

			if( winner != null )
			{
				CurrentMatch.Winner = CreateTeamDataFromTeam( winner , CurrentMatch );
			}

			GameDatabase.SaveMatchData( GameDatabase.SharedSettings.DateTimeToString( CurrentMatch.TimeStarted ) , CurrentMatch ).Wait();

			//also add this match to the globaldata as well
			MatchTracker.GlobalData globalData = GameDatabase.GetGlobalData().Result;
			globalData.Matches.Add( CurrentMatch.Name );

			//try adding the players from the matchdata into the globaldata

			foreach( PlayerData ply in CurrentMatch.Players )
			{
				if( !globalData.Players.Any( p => p.UserId == ply.UserId ) )
				{
					globalData.Players.Add( ply );
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