using MatchTracker;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Linq;

namespace MatchRecorder
{
	internal class ObsRecorder : IRecorder
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

		public ObsRecorder( MatchRecorderHandler parent )
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

		public void StartRecording() => requestedRecordingStart = true;

		public void StopRecording() => requestedRecordingStop = true;

		public void TryConnect()
		{
			try
			{
				obsHandler.Connect( MainHandler.OBSSettings.WebSocketUri , MainHandler.OBSSettings.WebSocketPassword );
			}
			catch( Exception )
			{
				DuckGame.HUD.AddCornerMessage( DuckGame.HUDCorner.TopRight , "Failed connecting to OBS. Check Settings/obs.json" );
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
			DuckGame.HUD.AddCornerMessage( DuckGame.HUDCorner.TopRight , "Connected to OBS!!!" );
		}

		private void OnDisconnected( object sender , EventArgs e )
		{
			DuckGame.HUD.AddCornerMessage( DuckGame.HUDCorner.TopRight , "Disconnected from OBS!!!" );
		}

		private void OnRecordingStateChanged( OBSWebsocket sender , OutputState type )
		{
			recordingState = type;
		}

		public void StartFrame()
		{

		}

		public void EndFrame()
		{
		}

		public void OnTextureDraw( DuckGame.Tex2D texture , DuckGame.Vec2 position , DuckGame.Rectangle? sourceRectangle , DuckGame.Color color , float rotation , DuckGame.Vec2 origin , DuckGame.Vec2 scale , int effects , DuckGame.Depth depth = default )
		{
		}

		public int OnStartStaticDraw()
		{
			return 0;
		}

		public void OnFinishStaticDraw()
		{
		}

		public void OnStaticDraw( int id )
		{
		}

		public bool WantsTexture( object tex )
		{
			return false;
		}

		public void SendTextureData( object tex , int width , int height , byte [] data )
		{
		}

		public void OnStartDrawingObject( object obj )
		{
		}

		public void OnFinishDrawingObject( object obj )
		{
		}
	}
}