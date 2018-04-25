using System;
using System.Linq;
using System.Reflection;
using Harmony;
using DuckGame;
using OBSWebsocketDotNet;
using System.IO;

namespace MatchRecorder
{
	public class MatchRecorderHandler
	{
		private OBSWebsocket obsHandler;

		private FileSystemWatcher videoFileSystemWatcher;

		private OutputState recordingState;
		private OutputState replayBufferState;

		//TODO: dunno what to do with these just yet
		private bool queuedRecording;
		private bool queuedReplayBuffer;

		private bool requestedReplayBufferSaveAndStop;
		private bool requestedReplayBufferStart;

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

		//TODO: this is fucking disgusting, fix later
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

		public MatchRecorderHandler()
		{
			recordingState = OutputState.Stopped;
			replayBufferState = OutputState.Stopped;
			queuedRecording = false;
			queuedReplayBuffer = false;

			obsHandler = new OBSWebsocket
			{
				WSTimeout = new TimeSpan( 0 , 0 , 1 , 0 , 0 )
			};
			obsHandler.Connected += OnConnected;
			obsHandler.Disconnected += OnDisconnected;
			//obsHandler.RecordingStateChanged += OnRecordingStateChanged;
			obsHandler.ReplayBufferStateChanged += OnReplayBufferStateChanged;
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

			InitFileSystemWatcher();
		}



		private void OnDisconnected( object sender , EventArgs e )
		{
			HUD.AddCornerMessage( HUDCorner.TopRight , "Disconnected from OBS!!!" );

			DeleteFileSystemWatcher();
		}


		private void OnRecordingStateChanged( OBSWebsocket sender , OutputState type )
		{
			recordingState = type;
		}


		private void OnReplayBufferStateChanged( OBSWebsocket sender , OutputState type )
		{
			replayBufferState = type;
			#region COMMENTEDCODE
			/*
			if( queuedReplayBuffer && replayBufferState == OutputState.Stopped )
			{
				StartReplayBuffer( true );
				queuedReplayBuffer = false;
			}
			*/
			#endregion COMMENTEDCODE
		}


		public void StartReplayBuffer( bool forced = false )
		{
			if( !obsHandler.IsConnected )
			{
				return;
			}

			if( !forced && replayBufferState == OutputState.Stopping )
			{
				queuedReplayBuffer = true;
				return;
			}

			try
			{
				obsHandler.StartReplayBuffer();
			}
			catch( Exception e )
			{

			}
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

		public void Update()
		{
			if( !obsHandler.IsConnected )
				return;

			//localized the try catches so that the variables wouldn't be set if an exception occurs, so it may try again on the next call
			switch( replayBufferState )
			{
				case OutputState.Started:
					{
						if( requestedReplayBufferSaveAndStop )
						{
							try
							{
								obsHandler.SaveReplayBuffer();
								
								//TODO: apparently this has priority somehow to the savereplaybuffer command!!! we need to call this after the replay is saved somehow
								//obsHandler.StopReplayBuffer();

								requestedReplayBufferSaveAndStop = false;
							}
							catch( Exception e )
							{

							}
						}

						break;
					}
				case OutputState.Stopped:
					{
						if( requestedReplayBufferStart )
						{
							try
							{
								obsHandler.StartReplayBuffer();
								requestedReplayBufferStart = false;
							}
							catch( Exception e )
							{

							}
						}
						break;
					}
			}

		}

		public void RequestStartReplayBuffer()
		{
			requestedReplayBufferStart = true;
		}

		public void RequestSaveAndStopReplayBuffer()
		{
			requestedReplayBufferSaveAndStop = true;
		}

		private void InitFileSystemWatcher()
		{
			videoFileSystemWatcher = new FileSystemWatcher( obsHandler.GetRecordingFolder() , "*.*" );
			videoFileSystemWatcher.Created += OnVideoCreated;
			
		}

		private void DeleteFileSystemWatcher()
		{
			if( videoFileSystemWatcher == null )
				return;

			videoFileSystemWatcher.Dispose();
			videoFileSystemWatcher = null;
		}

		private void OnVideoCreated( object sender , FileSystemEventArgs e )
		{
			//just in case the filesystemwatcher somehow doesn't get removed
			if( !obsHandler.IsConnected )
				return;

			

			try
			{
				obsHandler.StopReplayBuffer();
			}
			catch( Exception exc )
			{

			}

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
				Mod.GetRecorder().RequestStartReplayBuffer();
			}


			#region COMMENTEDCODE
			//if not recording start recording
			//Mod.GetRecorder().StartReplayBuffer();
			#endregion

		}
	}


	//save the video and stop recording
	[HarmonyPatch( typeof( Level ) , "set_current" )]
	class Level_SetCurrent
	{
		private static void Postfix( Level value )
		{
			//regardless if the current level can be recorded or not, we're done with the current recording so just save and stop
			if( Mod.GetRecorder().IsReplayBufferActive )
			{
				Mod.GetRecorder().RequestSaveAndStopReplayBuffer();
			}


			#region COMMENTEDCODE
			//if it's recording stop the recording
			/*
			if( Mod.GetRecorder().IsReplayBufferActive )
			{
				Mod.GetRecorder().SaveReplayBuffer();
				Mod.GetRecorder().StopReplayBuffer();
			}
			*/
			#endregion
		}
	}


}
