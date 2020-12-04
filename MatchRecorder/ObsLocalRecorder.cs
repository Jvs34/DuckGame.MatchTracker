﻿using MatchTracker;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Linq;

namespace MatchRecorder
{
	internal class ObsLocalRecorder : IRecorder
	{
		private MatchRecorderHandler MainHandler { get; }
		private DateTime nextObsCheck;
		private readonly OBSWebsocket obsHandler;
		private OutputState recordingState;
		private bool requestedRecordingStart;
		private bool requestedRecordingStop;

		public bool IsRecording
		{
			get
			{
				return recordingState switch
				{
					OutputState.Started or OutputState.Starting => true,
					OutputState.Stopped or OutputState.Stopping => false,
					_ => false,
				};
			}
		}

		public RecordingType ResultingRecordingType { get; set; }

		public ObsLocalRecorder( MatchRecorderHandler parent )
		{
			ResultingRecordingType = RecordingType.Video;
			MainHandler = parent;
			recordingState = OutputState.Stopped;
			obsHandler = new OBSWebsocket()
			{
				WSTimeout = TimeSpan.FromSeconds( 30 ) ,
			};

			obsHandler.Connected += OnConnected;
			obsHandler.Disconnected += OnDisconnected;
			obsHandler.RecordingStateChanged += OnRecordingStateChanged;
			TryConnect();
			nextObsCheck = DateTime.MinValue;
		}

		public void StartRecordingMatch() => MainHandler.StartCollectingMatchData();

		public void StopRecordingMatch() => MainHandler.StopCollectingMatchData();

		public void StartRecordingRound() => requestedRecordingStart = true;

		public void StopRecordingRound() => requestedRecordingStop = true;

		public void TryConnect()
		{
			try
			{
				obsHandler.Connect( MainHandler.OBSSettings.WebSocketUri , MainHandler.OBSSettings.WebSocketPassword );
			}
			catch( Exception )
			{
				MainHandler.ShowHUDmessage( "Failed connecting to OBS. Check Settings/obs.json" );
			}
		}

		/*
		public bool IsSetupCorrect()
		{
			var profile = obsHandler.GetCurrentProfile();

			if( profile != "Duck Game" )
			{
				return false;
			}

			var scene = obsHandler.GetCurrentScene();

			if( scene is null )
			{
				return false;
			}

			return scene.Name == "Duck Game";
		}

		public void SetupOBS()
		{
			obsHandler.SetCurrentProfile( "Duck Game" );

			//create the scene if it isn't there
			var scenes = obsHandler.GetSceneList();
			var duckgamescene = scenes.Scenes.FirstOrDefault( x => x.Name == "Duck Game" );
			
			if( duckgamescene is null )
			{
				obsHandler.Add
			}

		}
		*/

		public void Update()
		{
			if( !obsHandler.IsConnected )
			{
				//try reconnecting

				if( nextObsCheck < DateTime.Now )
				{
					TryConnect();

					nextObsCheck = DateTime.Now.AddSeconds( 5 );
				}

				return;
			}

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
								MainHandler.StopCollectingRoundData( endTime );
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
								string recordingTimeString = MainHandler.GameDatabase.SharedSettings.DateTimeToString( recordingTime );
								string roundPath = MainHandler.GameDatabase.SharedSettings.GetPath<RoundData>( recordingTimeString );

								//try setting the recording folder first, then create it before we start recording

								Directory.CreateDirectory( roundPath );
								obsHandler.SetRecordingFolder( roundPath );
								obsHandler.StartRecording();
								requestedRecordingStart = false;
								MainHandler.StartCollectingRoundData( recordingTime );
							}
							catch( Exception )
							{
							}
						}
						break;
					}
			}
		}

		private void OnConnected( object sender , EventArgs e )
		{
			MainHandler.ShowHUDmessage( "Connected to OBS." );
		}

		private void OnDisconnected( object sender , EventArgs e )
		{
			MainHandler.ShowHUDmessage( "Disconnected from OBS." );
		}

		private void OnRecordingStateChanged( OBSWebsocket sender , OutputState type )
		{
			recordingState = type;
		}
	}
}