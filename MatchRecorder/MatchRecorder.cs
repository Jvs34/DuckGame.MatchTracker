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
		private OBSWebsocket obsHandler;
		private static String roundNameFormat = "yyyy-MM-dd HH-mm-ss"; //this name format is the same as the one used by default on OBS Studio
		private OutputState recordingState;
		private bool requestedRecordingStop;
		private bool requestedRecordingStart;

		private MatchData currentMatch;
		private RoundData currentRound;
		private String currentFolder;
		private static String baseRecordingFolder = @"E:\DuckGameRecordings";//TODO:load this from settings
		private bool lastIsGameInProgress;

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


		public MatchRecorderHandler()
		{

			lastIsGameInProgress = false;
			recordingState = OutputState.Stopped;
			obsHandler = new OBSWebsocket()
			{
				WSTimeout = new TimeSpan( 0 , 0 , 1 , 0 , 0 ) ,
			};

			obsHandler.Connected += OnConnected;
			obsHandler.Disconnected += OnDisconnected;
			obsHandler.RecordingStateChanged += OnRecordingStateChanged;

		}

		public void Init()
		{
			//TODO: we will use a password later, but we will read it from secrets.json or something since that will also be required by the youtube uploader

			try
			{
				obsHandler.Connect( "ws://127.0.0.1:4444" , "imgay" );
			}
			catch( Exception e )
			{

			}

		}

		//only record game levels for now since we're kind of tied to the gounvirtual stuff
		public bool IsLevelRecordable( Level level )
		{
			return level is GameLevel;
		}

		public static String GetRoundName( DateTime time )
		{
			return time.ToString( roundNameFormat );
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
				return;

			var teams = Teams.core;
			var core = Level.core;

			bool isGameInProgress = false;

			var eventsList = Event.events;

			int gay = 5 + 1;
			gay = gay * 2;
			//for local multiplayer we have to use a different variable
			if( Network.isActive )
			{
				//isGameInProgress = Level.core.gameInProgress;
			}
			else
			{
				/*
				int count = 0;

				foreach( Team team in Teams.all )
				{
					count += team.activeProfiles.Count;
				}

				isGameInProgress = count > 0;
				*/
			}

			if( lastIsGameInProgress != isGameInProgress )
			{
				if( isGameInProgress )
				{
					StartCollectingMatchData();
				}
				else
				{
					StopCollectingMatchData();
				}

				lastIsGameInProgress = isGameInProgress;
			}

			//I don't think this variable is used at all in multiplayer
			if( Level.core.gameFinished )
			{

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
							catch( Exception e )
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

								currentFolder = Path.Combine( baseRecordingFolder , GetRoundName( recordingTime ) );
								//try setting the recording folder first, then create it before we start recording

								Directory.CreateDirectory( currentFolder );

								obsHandler.SetRecordingFolder( currentFolder );

								obsHandler.StartRecording();
								requestedRecordingStart = false;
								StartCollectingRoundData( recordingTime );
							}
							catch( Exception e )
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

			foreach( Profile pro in Profiles.active )
			{
				currentRound.players.Add( CreatePlayerDataFromProfile( pro ) );
			}


			if( lvl is GameLevel gl )
			{
				currentRound.isCustomLevel = gl.isCustomLevel;
			}

			//TODO: add the name of the round to the MatchData
			if( currentMatch != null )
			{
				currentMatch.rounds.Add( GetRoundName( currentRound.timeStarted ) );
			}


		}


		private void StopCollectingRoundData( DateTime endTime )
		{
			if( currentRound == null )
			{
				return;
			}
			currentRound.timeEnded = endTime; 

			String filePath = currentFolder;
			filePath = Path.Combine( filePath , "rounddata" );

			String jsonOutput = JsonConvert.SerializeObject( currentRound , Formatting.Indented );

			File.WriteAllText( Path.ChangeExtension( filePath , "json" ) , jsonOutput );


			currentRound = null;
		}

		public void TryCollectingMatchData()
		{
			//try saving the match if there's one and it's got at least one round
			if( currentMatch != null )
			{
				if( currentMatch.rounds.Count > 0 )
				{
					StopCollectingMatchData();
				}
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
				players = new List<PlayerData>(),
			};
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
				winner = Teams.winning.First<Team>();
			}

			if( winner != null )
			{
				currentMatch.winner = CreateTeamDataFromTeam( winner );
			}

			foreach( Profile pro in Profiles.active )
			{
				currentMatch.players.Add( CreatePlayerDataFromProfile( pro ) );
			}


			String filePath = baseRecordingFolder;
			filePath = Path.Combine( filePath , "matchdata " + GetRoundName( currentMatch.timeStarted ) );

			String jsonOutput = JsonConvert.SerializeObject( currentMatch , Formatting.Indented );

			File.WriteAllText( Path.ChangeExtension( filePath , "json" ) , jsonOutput );

			currentMatch = null;
		}

		private TeamData CreateTeamDataFromTeam( Team team )
		{
			TeamData td = new TeamData()
			{
				hasHat = team.hasHat ,
				wins = team.wins ,
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

			return pd;
		}

	}

	[HarmonyPatch( typeof( Level ) , nameof( Level.UpdateCurrentLevel ) )]
	class UpdateLoop
	{
		private static void Prefix()
		{
			if( Mod.GetRecorder() != null )
			{
				Mod.GetRecorder().Update();
			}
		}
	}


	//start recording
	[HarmonyPatch( typeof( VirtualTransition ) , nameof( VirtualTransition.GoUnVirtual ) )]
	class VirtualTransition_GoUnVirtual
	{
		private static void Postfix()
		{
			//only bother if the current level is something we care about
			if( Mod.GetRecorder().IsLevelRecordable( Level.current ) )
			{
				Mod.GetRecorder().StartRecording();
			}

		}
	}


	//save the video and stop recording
	[HarmonyPatch( typeof( Level ) , "set_current" )]
	class Level_SetCurrent
	{
		//changed the Postfix to a Prefix so we can get the Level.current before it's changed to the new one
		private static void Prefix( Level value )
		{
			//regardless if the current level can be recorded or not, we're done with the current recording so just save and stop
			if( Mod.GetRecorder().IsRecording )
			{
				Mod.GetRecorder().StopRecording();
			}

		}
	}


	//this is called once when the
	[HarmonyPatch( typeof( Main ) , "ResetMatchStuff" )]
	class Main_ResetMatchStuff
	{
		private static void Prefix()
		{
			if( Mod.GetRecorder() != null )
			{
				Mod.GetRecorder().TryCollectingMatchData();
			}
		}
	}


}
