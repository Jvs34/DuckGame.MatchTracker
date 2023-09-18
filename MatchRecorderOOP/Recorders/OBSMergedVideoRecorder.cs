using MatchRecorder.Services;
using MatchRecorderShared;
using MatchTracker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsStrawket;
using ObsStrawket.DataTypes.Predefineds;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace MatchRecorder.Recorders
{
	/// <summary>
	/// Records an entire match at once, alongside pauses, transitions and even the victory screen,
	/// however, data wise only the singular rounds are recorded
	/// </summary>
	internal sealed class OBSMergedVideoRecorder : BaseRecorder
	{
		private OBSSettings OBSSettings { get; }
		private ObsClientSocket ObsHandler { get; }
		private ObsOutputState RecordingState { get; set; }
		public override bool IsRecording => RecordingState switch
		{
			ObsOutputState.Started or ObsOutputState.Starting => true,
			ObsOutputState.Stopped or ObsOutputState.Stopping => false,
			_ => false,
		};
		public override RecordingType ResultingRecordingType { get; set; }
		private DateTime NextObsCheck { get; set; }

		public OBSMergedVideoRecorder(
			ILogger<BaseRecorder> logger ,
			ModMessageQueue messageQueue ,
			IOptions<OBSSettings> obsSettings ,
			IGameDatabase db ) : base( logger , db , messageQueue )
		{
			ResultingRecordingType = RecordingType.Video;
			OBSSettings = obsSettings.Value;
			GameDatabase = db;
			RecordingState = ObsOutputState.Stopped;
			ObsHandler = new ObsClientSocket();
			ObsHandler.Connected += OnConnected;
			ObsHandler.Disconnected += OnDisconnected;
			ObsHandler.RecordStateChanged += OnRecordingStateChanged;
			NextObsCheck = DateTime.MinValue;
		}

		/// <summary>
		/// A workaround to the fact that some of the methods in obs-websocket are non-blocking
		/// and for instance, don't await until recording fully starts.
		/// </summary>
		/// <param name="desiredRecordingState"></param>
		/// <returns></returns>
		private async Task WaitUntilRecordingState( ObsOutputState desiredRecordingState )
		{
			using var timeoutCancellationTokenSource = new CancellationTokenSource( TimeSpan.FromSeconds( 10 ) );

			//https://stackoverflow.com/questions/31787840/taskcompletionsource-throws-an-attempt-was-made-to-transition-a-task-to-a-final
			var tempTaskCompletionSource = new TaskCompletionSource<bool>();

			void recordStateChangedDelegate( RecordStateChanged recordStateChanged )
			{
				if( recordStateChanged.OutputState == desiredRecordingState )
				{
					ObsHandler.RecordStateChanged -= recordStateChangedDelegate;
					tempTaskCompletionSource.TrySetResult( true );
				}
			}

			//a timeout just in case
			timeoutCancellationTokenSource.Token.Register( () =>
			{
				ObsHandler.RecordStateChanged -= recordStateChangedDelegate;
				tempTaskCompletionSource.TrySetCanceled( timeoutCancellationTokenSource.Token );
			} );

			ObsHandler.RecordStateChanged += recordStateChangedDelegate;

			await tempTaskCompletionSource.Task;
		}

		/// <summary>
		/// Needed for now as the stable OBS doesn't have an up to date obs-websocket
		/// and calling handler.SetRecordDirectory throws an error as it doesn't exist in that version
		/// </summary>
		/// <param name="recordingFolder"></param>
		/// <returns></returns>
		private async Task SetRecordDirectoryWorkaroundAsync( string recordingFolder )
		{
			await ObsHandler.SetProfileParameterAsync( "AdvOut" , "RecFilePath" , recordingFolder );
			await ObsHandler.SetProfileParameterAsync( "SimpleOutput" , "FilePath" , recordingFolder );
		}

		protected override async Task StartRecordingMatchInternal()
		{
			DateTime recordingTime = DateTime.Now;
			string recordingTimeString = GameDatabase.SharedSettings.DateTimeToString( recordingTime );
			string matchPath = GameDatabase.SharedSettings.GetPath<MatchData>( recordingTimeString );

			Directory.CreateDirectory( matchPath );

			await SetRecordDirectoryWorkaroundAsync( matchPath );
			await ObsHandler.StartRecordAsync();
			await WaitUntilRecordingState( ObsOutputState.Started );

			var match = await StartCollectingMatchData( recordingTime );

			var videoUpload = new VideoUpload()
			{
				VideoType = VideoUrlType.MergedVideoLink,
			};
			match.VideoUploads.Add( videoUpload );

			await GameDatabase.SaveData( match );
		}

		protected override async Task StopRecordingMatchInternal()
		{
			DateTime endTime = DateTime.Now;
			await ObsHandler.StopRecordAsync();
			await WaitUntilRecordingState( ObsOutputState.Stopped );
			var match = await StopCollectingMatchData( endTime );
		}

		protected override async Task StartRecordingRoundInternal()
		{
			var round = await StartCollectingRoundData( DateTime.Now );

			var videoUpload = new VideoUpload()
			{
				VideoType = VideoUrlType.MergedVideoLink ,
			};
			round.VideoUploads.Add( videoUpload );
		}

		protected override async Task StopRecordingRoundInternal() => await StopCollectingRoundData( DateTime.Now );

		public async Task TryConnect()
		{
			if( ObsHandler.IsConnected )
			{
				return;
			}

			try
			{
				await ObsHandler.ConnectAsync( new Uri( OBSSettings.WebSocketUri ) , OBSSettings.WebSocketPassword );
			}
			catch( Exception )
			{
				SendHUDmessage( "Failed connecting to OBS. Check Settings/obs.json" , TextMessagePosition.TopMiddle );
			}
		}

		public override async Task Update()
		{
			if( NextObsCheck < DateTime.Now )
			{
				await TryConnect();

				NextObsCheck = DateTime.Now.AddSeconds( 5 );
			}
		}

		private void OnConnected( Uri uri ) => SendHUDmessage( "Connected to OBS." );
		private void OnDisconnected( Exception exception ) => SendHUDmessage( "Disconnected from OBS." );
		private void OnRecordingStateChanged( RecordStateChanged changed ) => RecordingState = changed.OutputState;

	}
}