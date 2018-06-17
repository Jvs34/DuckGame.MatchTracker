using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using DuckGame;
using OBSWebsocketDotNet;
using System.IO;
using MatchTracker;
using Newtonsoft.Json;

namespace MatchRecorder
{
	public class MatchRecorderHandler
	{
		private SharedSettings sharedSettings;
		private OBSWebsocket obsHandler;
		private OutputState recordingState;
		private bool requestedRecordingStop;
		private bool requestedRecordingStart;

		private MatchData currentMatch;
		private RoundData currentRound;

		private string roundsFolder;
		private string matchesFolder;

		//TODO: this is fucking disgusting, fix later
		public bool IsRecording
		{
			get
			{
				switch( recordingState )
				{
					case OutputState.Started:
					case OutputState.Starting:
						return true;

					case OutputState.Stopped:
					case OutputState.Stopping:
						return false;
				}

				return false;
			}
		}


		public MatchRecorderHandler( String modPath )
		{
			sharedSettings = new SharedSettings();
			String sharedSettingsPath = Path.Combine( Path.Combine( modPath , "Settings" ) , "shared.json" );

			sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );

			recordingState = OutputState.Stopped;
			obsHandler = new OBSWebsocket()
			{
				WSTimeout = new TimeSpan( 0 , 0 , 1 , 0 , 0 ) ,
			};

			obsHandler.Connected += OnConnected;
			obsHandler.Disconnected += OnDisconnected;
			obsHandler.RecordingStateChanged += OnRecordingStateChanged;

			roundsFolder = Path.Combine( sharedSettings.GetRecordingFolder() , sharedSettings.roundsFolder );
			matchesFolder = Path.Combine( sharedSettings.GetRecordingFolder() , sharedSettings.matchesFolder );

			if( !Directory.Exists( roundsFolder ) )
				Directory.CreateDirectory( roundsFolder );

			if( !Directory.Exists( matchesFolder ) )
				Directory.CreateDirectory( matchesFolder );


			//TODO: we will use a password later, but we will read it from secrets.json or something since that will also be required by the youtube uploader
			try
			{
				obsHandler.Connect( "ws://127.0.0.1:4444" , "imgay" );
			}
			catch( Exception )
			{
				HUD.AddCornerMessage( HUDCorner.BottomRight , "Could not connect to OBS!!!" );
			}
		}

		public void Init()
		{



		}

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

		private void OnRecordingStateChanged( OBSWebsocket sender , OutputState type )
		{
			recordingState = type;
		}

		public void Update()
		{
			if( !obsHandler.IsConnected )
			{
				return;
			}

			//localized the try catches so that the variables wouldn't be set if an exception occurs, so it may try again on the next call
			switch( recordingState )
			{
				case OutputState.Started:
					{
						if( requestedRecordingStop )
						{
							try
							{
								DateTime endTime = DateTime.Now;
								obsHandler.StopRecording();
								requestedRecordingStop = false;
								StopCollectingRoundData( endTime );
							}
							catch( Exception )
							{

							}
						}

						break;
					}
				case OutputState.Stopped:
					{
						if( requestedRecordingStart )
						{
							try
							{
								DateTime recordingTime = DateTime.Now;
								String roundPath = Path.Combine( roundsFolder , sharedSettings.DateTimeToString( recordingTime ) );
								//try setting the recording folder first, then create it before we start recording

								Directory.CreateDirectory( roundPath );

								obsHandler.SetRecordingFolder( roundPath );

								obsHandler.StartRecording();
								requestedRecordingStart = false;
								StartCollectingRoundData( recordingTime );
							}
							catch( Exception )
							{

							}
						}
						break;
					}
			}

		}


		public void StopRecording()
		{
			requestedRecordingStop = true;
		}

		public void StartRecording()
		{
			requestedRecordingStart = true;
		}

		private void StartCollectingRoundData( DateTime startTime )
		{
			Level lvl = Level.current;

			currentRound = new RoundData()
			{
				levelName = lvl.level ,
				players = new List<PlayerData>() ,
				timeStarted = startTime ,
				isCustomLevel = false ,
			};

			currentRound.roundName = sharedSettings.DateTimeToString( currentRound.timeStarted );

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
				currentMatch.rounds.Add( sharedSettings.DateTimeToString( currentRound.timeStarted ) );
			}


		}


		private void StopCollectingRoundData( DateTime endTime )
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

			sharedSettings.SaveRoundData( sharedSettings.DateTimeToString( currentRound.timeStarted ) , currentRound );

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

			currentMatch.matchName = sharedSettings.DateTimeToString( currentMatch.timeStarted );
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


			sharedSettings.SaveMatchData( sharedSettings.DateTimeToString( currentMatch.timeStarted ) , currentMatch );
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

			//TODO: multiple local players made by the host have the same steamid, try to use a different method for those guys
			//this does not actually work properly
			/*
			if( Network.isActive && profile.linkedProfile != null )
			{
				//try to convert the profile name into something like like PLAYER2
				int netIndex = profile.networkIndex + 1;
				pd.userId = "Profile" + netIndex;
			}
			*/


			return pd;
		}

	}

	[HarmonyPatch( typeof( Level ) , nameof( Level.UpdateCurrentLevel ) )]
	internal static class UpdateLoop
	{
		private static void Prefix()
		{
			//if( Mod.Recorder != null )
			{
				Mod.Recorder?.Update();
			}
		}
	}


	//start recording
	[HarmonyPatch( typeof( VirtualTransition ) , nameof( VirtualTransition.GoUnVirtual ) )]
	internal static class VirtualTransition_GoUnVirtual
	{
		private static void Postfix()
		{
			//only bother if the current level is something we care about
			if( Mod.Recorder.IsLevelRecordable( Level.current ) )
			{
				Mod.Recorder.StartRecording();
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
			if( Mod.Recorder.IsRecording )
			{
				Mod.Recorder.StopRecording();
			}

			//only really useful in multiplayer, since continuing a match from the endgame screen doesn't trigger ResetMatchStuff on other clients
			//so unfortunately we have to do this hack
			if( Network.isActive && Network.isClient && !Level.core.gameInProgress )
			{
				Level oldValue = Level.current;
				Level newValue = value;
				if( oldValue is RockScoreboard && Mod.Recorder.IsLevelRecordable( newValue ) )
				{
					Mod.Recorder?.TryCollectingMatchData();
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
			//if( Mod.Recorder != null )
			{
				Mod.Recorder?.TryCollectingMatchData();
			}
		}
	}


}
