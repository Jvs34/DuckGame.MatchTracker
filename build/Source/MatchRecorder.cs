using System;
using System.Linq;
using System.Reflection;
using Harmony;
using DuckGame;
using OBSWebsocketDotNet;

namespace MatchRecorder
{
	public class MatchRecorderHandler
	{
		private OBSWebsocket obsHandler;
		private OutputState recordingState;
		private OutputState replayBufferState;

		public MatchRecorderHandler()
		{
			recordingState = OutputState.Stopped;
			replayBufferState = OutputState.Stopped;

			obsHandler = new OBSWebsocket
			{
				WSTimeout = new TimeSpan( 0 , 0 , 1 , 0 , 0 )
			};
			obsHandler.Connected += OnConnected;
			obsHandler.Disconnected += OnDisconnected;
			//obsHandler.RecordingStateChanged += OnRecordingStateChanged;
			obsHandler.ReplayBufferStateChanged += OnReplayBufferStateChanged;

		}



		public bool IsRecording {
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
		public bool IsReplayBufferActive
		{
			get
			{
				switch( replayBufferState )
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

		public void Init()
		{
			//TODO: we will use a password later, but we will read it from secrets.json or something since that will also be required by the youtube uploader

			try
			{
				obsHandler.Connect( "ws://127.0.0.1:4444" , "" );
			}
			catch( Exception e )
			{

			}

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


		private void OnReplayBufferStateChanged( OBSWebsocket sender , OutputState type )
		{
			replayBufferState = type;
		}

		//we call this function from the level start regardless i
		public void StartReplayBuffer()
		{
			if( !obsHandler.IsConnected )
				return;


			try
			{
				obsHandler.StartReplayBuffer();
			}
			catch( Exception e )
			{

			}
			//obsHandler.StartRecording();
		}

		public void SaveReplayBuffer()
		{
			if( !obsHandler.IsConnected )
				return;

			try
			{
				obsHandler.SaveReplayBuffer();
			}
			catch( Exception e )
			{

			}
		}

		public void StopReplayBuffer()
		{
			if( !obsHandler.IsConnected )
				return;

			try
			{
				obsHandler.StopReplayBuffer();
			}
			catch( Exception e )
			{

			}
		}

	}





	[HarmonyPatch( typeof( Level ) , "set_current" )]
	class Level_SetCurrent
	{
		private static void Postfix( Level value )
		{
			//if it's recording stop the recording
			if( Mod.GetRecorder().IsReplayBufferActive )
			{
				Mod.GetRecorder().SaveReplayBuffer();
				//and if this is a level we don't like, also stop the replay buffer
				if ( !(value is GameLevel) )
				{
					Mod.GetRecorder().StopReplayBuffer();
				}
			}


		}
	}

	[HarmonyPatch( typeof( VirtualTransition ) , nameof( VirtualTransition.GoUnVirtual ) )]
	class VirtualTransition_GoUnVirtual
	{
		private static void Postfix()
		{
			//if not recording start recording
			if( !Mod.GetRecorder().IsReplayBufferActive )
			{
				Mod.GetRecorder().StartReplayBuffer();
			}
		}
	}
}
