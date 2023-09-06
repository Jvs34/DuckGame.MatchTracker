using MatchTracker;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Linq;


namespace MatchRecorder
{
	internal sealed class ObsLocalRecorder : IRecorder
	{
		private MatchRecorderService MainHandler { get; }
		private OBSWebsocket ObsHandler { get; }
		private OutputState RecordingState { get; set; }
		private bool RequestedRecordingStart { get; set; }
		private bool RequestedRecordingStop { get; set; }
		public bool IsRecording => RecordingState switch
		{
			OutputState.Started or OutputState.Starting => true,
			OutputState.Stopped or OutputState.Stopping => false,
			_ => false,
		};
		public RecordingType ResultingRecordingType { get; set; }
		private DateTime NextObsCheck { get; set; }
		private TimeSpan MergedRoundDuration { get; set; } = TimeSpan.Zero;


		public ObsLocalRecorder( MatchRecorderService parent )
		{
			ResultingRecordingType = RecordingType.Video;
			MainHandler = parent;
			RecordingState = OutputState.Stopped;
			ObsHandler = new OBSWebsocket()
			{
				WSTimeout = TimeSpan.FromSeconds( 30 ) ,
			};

			ObsHandler.Connected += OnConnected;
			ObsHandler.Disconnected += OnDisconnected;
			ObsHandler.RecordingStateChanged += OnRecordingStateChanged;
			TryConnect();
			NextObsCheck = DateTime.MinValue;
		}

		public void StartRecordingMatch()
		{
			RequestedRecordingStart = true;
			/*
			var match = MainHandler.StartCollectingMatchData();
			match.VideoType = VideoType.MergedVideoLink;
			*/
		}

		public void StopRecordingMatch()
		{
			RequestedRecordingStop = true;
			//MainHandler.StopCollectingMatchData();
		}

		public void StartRecordingRound()
		{
			var round = MainHandler.StartCollectingRoundData( DateTime.Now );
			if( round is null )
			{
				return;
			}
			round.VideoType = VideoType.MergedVideoLink;
			round.VideoStartTime = MergedRoundDuration;
		}

		public void StopRecordingRound()
		{
			var round = MainHandler.StopCollectingRoundData( DateTime.Now );
			if( round is null )
			{
				return;
			}

			MergedRoundDuration += round.GetDuration();
			round.VideoEndTime = MergedRoundDuration;
		}

		public void TryConnect()
		{
			try
			{
				ObsHandler.Connect( MainHandler.OBSSettings.WebSocketUri , MainHandler.OBSSettings.WebSocketPassword );
			}
			catch( Exception )
			{
				MainHandler.ShowHUDmessage( "Failed connecting to OBS. Check Settings/obs.json" );
			}
		}

		public void Update()
		{
			if( !ObsHandler.IsConnected )
			{
				//try reconnecting

				if( NextObsCheck < DateTime.Now )
				{
					TryConnect();

					NextObsCheck = DateTime.Now.AddSeconds( 5 );
				}

				return;
			}

			switch( RecordingState )
			{
				case OutputState.Started:
					{
						if( RequestedRecordingStop )
						{
							try
							{
								DateTime endTime = DateTime.Now;
								ObsHandler.StopRecording();
								RequestedRecordingStop = false;
								var match = MainHandler.StopCollectingMatchData( endTime );
								if( match is null )
								{
									break;
								}

							}
							catch( Exception )
							{
							}
						}


						break;
					}
				case OutputState.Stopped:
					{
						if( RequestedRecordingStart )
						{
							try
							{
								DateTime recordingTime = DateTime.Now;
								string recordingTimeString = MainHandler.GameDatabase.SharedSettings.DateTimeToString( recordingTime );
								string matchPath = MainHandler.GameDatabase.SharedSettings.GetPath<MatchData>( recordingTimeString );

								//try setting the recording folder first, then create it before we start recording

								Directory.CreateDirectory( matchPath );
								ObsHandler.SetRecordingFolder( matchPath );
								ObsHandler.StartRecording();
								RequestedRecordingStart = false;
								var match = MainHandler.StartCollectingMatchData( recordingTime );

								if( match is null )
								{
									break;
								}

								match.VideoType = VideoType.MergedVideoLink;
								match.VideoEndTime = match.GetDuration();
								MergedRoundDuration = TimeSpan.Zero;

								MainHandler.GameDatabase.SaveData( match ).Wait();
							}
							catch( Exception )
							{
							}
						}

						break;
					}
			}
		}

		private void OnConnected( object sender , EventArgs e ) => MainHandler.ShowHUDmessage( "Connected to OBS." );
		private void OnDisconnected( object sender , EventArgs e ) => MainHandler.ShowHUDmessage( "Disconnected from OBS." );
		private void OnRecordingStateChanged( OBSWebsocket sender , OutputState type ) => RecordingState = type;
	}
}