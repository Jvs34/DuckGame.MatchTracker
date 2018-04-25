using System;
using System.Linq;
using System.Reflection;
using Harmony;
using DuckGame;
using OBSWebsocketDotNet;
using System.IO;
//using MatchTracker;


namespace MatchRecorder
{
	public class MatchRecorderHandler
	{
		private OBSWebsocket obsHandler;
		private OutputState recordingState;
		private bool requestedRecordingStop;
		private bool requestedRecordingStart;

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
			recordingState = OutputState.Stopped;
			obsHandler = new OBSWebsocket
			{
				WSTimeout = new TimeSpan( 0 , 0 , 1 , 0 , 0 )
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

		//only record game levels for now
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
				return;

			if( Level.core.gameInProgress )
			{

			}

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
								obsHandler.StopRecording();
								requestedRecordingStop = false;
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
								obsHandler.StartRecording();
								requestedRecordingStart = false;
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
			StartCollectingRoundData();
		}


		public void StartRecording()
		{
			requestedRecordingStart = true;
			StopCollectingRoundData();
		}

		private void StartCollectingRoundData()
		{
		}


		private void StopCollectingRoundData()
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
		private static void Postfix( Level value )
		{
			//regardless if the current level can be recorded or not, we're done with the current recording so just save and stop
			if( Mod.GetRecorder().IsRecording )
			{
				Mod.GetRecorder().StopRecording();
			}

		}
	}


}
