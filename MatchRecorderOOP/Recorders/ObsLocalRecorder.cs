using MatchRecorder.Services;
using MatchTracker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebSocketSharp;

namespace MatchRecorder.Recorders
{
	internal sealed class ObsLocalRecorder : BaseRecorder
	{
		public OBSSettings OBSSettings { get; }
		private OBSWebsocket ObsHandler { get; }
		private OutputState RecordingState { get; set; }
		private bool RequestedRecordingStart { get; set; }
		private bool RequestedRecordingStop { get; set; }
		public override bool IsRecording => RecordingState switch
		{
			OutputState.Started or OutputState.Starting => true,
			OutputState.Stopped or OutputState.Stopping => false,
			_ => false,
		};
		public override RecordingType ResultingRecordingType { get; set; }
		private DateTime NextObsCheck { get; set; }
		private TimeSpan MergedRoundDuration { get; set; } = TimeSpan.Zero;

		public ObsLocalRecorder(
			ILogger<IRecorder> logger ,
			ModMessageQueue messageQueue,
			IOptions<OBSSettings> obsSettings ,
			IGameDatabase db ) : base( logger , db , messageQueue )
		{
			ResultingRecordingType = RecordingType.Video;
			OBSSettings = obsSettings.Value;
			GameDatabase = db;
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

		public override async Task StartRecordingMatch()
		{
			RequestedRecordingStart = true;
			/*
			var match = MainHandler.StartCollectingMatchData();
			match.VideoType = VideoType.MergedVideoLink;
			*/
		}

		public override async Task StopRecordingMatch()
		{
			RequestedRecordingStop = true;
			//MainHandler.StopCollectingMatchData();
		}

		public override async Task StartRecordingRound()
		{
			var round = await StartCollectingRoundData( DateTime.Now );
			if( round is null )
			{
				return;
			}
			round.VideoType = VideoType.MergedVideoLink;
			round.VideoStartTime = MergedRoundDuration;
		}

		public override async Task StopRecordingRound()
		{
			var round = await StopCollectingRoundData( DateTime.Now );
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
				ObsHandler.Connect( OBSSettings.WebSocketUri , OBSSettings.WebSocketPassword );
			}
			catch( Exception )
			{
				SendHUDmessage( "Failed connecting to OBS. Check Settings/obs.json" );
			}
		}

		public override async Task Update()
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
								var match = await StopCollectingMatchData( endTime );
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
								string recordingTimeString = GameDatabase.SharedSettings.DateTimeToString( recordingTime );
								string matchPath = GameDatabase.SharedSettings.GetPath<MatchData>( recordingTimeString );

								//try setting the recording folder first, then create it before we start recording

								Directory.CreateDirectory( matchPath );
								ObsHandler.SetRecordingFolder( matchPath );
								ObsHandler.StartRecording();
								RequestedRecordingStart = false;
								var match = await StartCollectingMatchData( recordingTime );

								if( match is null )
								{
									break;
								}

								match.VideoType = VideoType.MergedVideoLink;
								match.VideoEndTime = match.GetDuration();
								MergedRoundDuration = TimeSpan.Zero;

								GameDatabase.SaveData( match ).Wait();
							}
							catch( Exception )
							{
							}
						}

						break;
					}
			}
		}

		private void OnConnected( object sender , EventArgs e ) => SendHUDmessage( "Connected to OBS." );
		private void OnDisconnected( object sender , EventArgs e ) => SendHUDmessage( "Disconnected from OBS." );
		private void OnRecordingStateChanged( OBSWebsocket sender , OutputState type ) => RecordingState = type;
	}
}