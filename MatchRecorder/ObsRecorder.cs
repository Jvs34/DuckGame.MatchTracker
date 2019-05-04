using MatchTracker;
using OBSWebsocketDotNet;
using System;
using System.IO;

namespace MatchRecorder
{
	internal class ObsRecorder : IRecorder
	{
		private readonly MatchRecorderHandler mainHandler;
		private DateTime nextObsCheck;
		private OBSWebsocket obsHandler;
		private OutputState recordingState;
		private bool requestedRecordingStart;
		private bool requestedRecordingStop;

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
			//do nothing
			private set
			{
			}
		}

		public RecordingType ResultingRecordingType { get; set; }

		public ObsRecorder( MatchRecorderHandler parent )
		{
			ResultingRecordingType = RecordingType.Video;
			mainHandler = parent;
			recordingState = OutputState.Stopped;
			obsHandler = new OBSWebsocket()
			{
				WSTimeout = TimeSpan.FromSeconds( 30 ) ,
			};

			obsHandler.Connected += OnConnected;
			obsHandler.Disconnected += OnDisconnected;
			obsHandler.RecordingStateChanged += OnRecordingStateChanged;
			//TODO: we will use a password later, but we will read it from secrets.json or something since that will also be required by the youtube uploader
			TryConnect();
			nextObsCheck = DateTime.MinValue;
		}

		public void StartRecording() => requestedRecordingStart = true;

		public void StopRecording() => requestedRecordingStop = true;

		public void TryConnect()
		{
			try
			{
				obsHandler.Connect( "ws://127.0.0.1:4444" , "imgay" );
			}
			catch( Exception )
			{
				DuckGame.HUD.AddCornerMessage( DuckGame.HUDCorner.TopRight , "Could not connect to OBS!!!" );
			}
		}

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
								mainHandler.StopCollectingRoundData( endTime );
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
								string roundPath = Path.Combine( mainHandler.RoundsFolder , mainHandler.GameDatabase.SharedSettings.DateTimeToString( recordingTime ) );
								//try setting the recording folder first, then create it before we start recording

								Directory.CreateDirectory( roundPath );

								obsHandler.SetRecordingFolder( roundPath );

								obsHandler.StartRecording();
								requestedRecordingStart = false;
								mainHandler.StartCollectingRoundData( recordingTime );
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
	}
}