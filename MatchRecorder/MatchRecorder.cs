using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using DuckGame;
using OBSWebsocketDotNet;
using System.IO;
using MatchTracker;


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


			//TODO: get another bool and do the flipflop kind of thing to start tracking when a match starts and ends
			bool isGameInProgress = Level.core.gameInProgress;
			if( lastIsGameInProgress != isGameInProgress )
			{
				if (isGameInProgress)
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
				timeStarted = startTime , //TODO: replace with UtcNow?
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

			String filePath = currentFolder;
			filePath = Path.Combine( filePath , "rounddata" );

			//as a test just write a file with the same name as the video file
			File.WriteAllText( Path.ChangeExtension( filePath , "json" ) , "im gay" );
		}


		private void StopCollectingRoundData( DateTime endTime )
		{
			if( currentRound == null )
			{
				return;
			}
			currentRound.timeEnded = endTime; //TODO: replace with UtcNow?
												   //write to file
			
			currentRound = null;
		}

		private void StartCollectingMatchData()
		{
			currentMatch = new MatchData
			{
				timeStarted = DateTime.Now, //TODO: replace with UtcNow?
				
			};


		}

		private void StopCollectingMatchData()
		{
			if( currentMatch == null )
			{
				return;
			}
			currentMatch.timeEnded = DateTime.Now;

			//TODO:save match
			currentMatch = null;
		}

		private PlayerData CreatePlayerDataFromProfile( Profile profile )
		{
			PlayerData pd = new PlayerData
			{
				userId = profile.steamID.ToString() ,
				name = profile.name ,
				nickName = profile.rawName ,
				team = new HatData()
				{
					hatName = profile.team.name ,
					isCustomHat = profile.team.customData != null
				}
			};

			return pd;
		}

		private void WriteMatchToFile()
		{

		}

		private void WriteRoundToFile()
		{

		}
	}

	[HarmonyPatch( typeof( Level ) , nameof( Level.UpdateCurrentLevel ) )]
	class UpdateLoop
	{
		private static void Prefix()
		{
			if( Mod.GetRecorder() == null )
			{
				return;
			}

			Mod.GetRecorder().Update();
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


}
