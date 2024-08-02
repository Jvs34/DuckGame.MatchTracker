using MatchRecorder.Shared.Enums;
using MatchRecorder.Shared.Settings;
using MatchShared.Databases.Interfaces;
using MatchShared.DataClasses;
using MatchShared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ObsStrawket;
using ObsStrawket.DataTypes.Predefineds;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder.OOP.Recorders;

/// <summary>
/// Records an entire match at once, alongside pauses, transitions and even the victory screen,
/// however, data wise only the singular rounds are recorded
/// </summary>
internal class OBSRawVideoRecorder : BaseRecorder
{
	protected OBSSettings OBSSettings { get; }
	protected ObsClientSocket ObsHandler { get; }
	protected ObsOutputState RecordingState { get; set; }
	public override bool IsRecording => RecordingState switch
	{
		ObsOutputState.Started or ObsOutputState.Starting => true,
		ObsOutputState.Stopped or ObsOutputState.Stopping => false,
		_ => false,
	};
	public override RecordingType ResultingRecordingType { get; set; }
	protected DateTime NextObsCheck { get; set; }

	public OBSRawVideoRecorder(
		ILogger<BaseRecorder> logger,
		ModMessageQueue messageQueue,
		IOptions<OBSSettings> obsSettings,
		IGameDatabase db ) : base( logger, db, messageQueue )
	{
		ResultingRecordingType = RecordingType.Video;
		RecorderConfigType = RecorderType.OBSRawVideo;
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

	protected override async Task StartRecordingMatchInternal()
	{
		DateTime recordingTime = DateTime.Now;
		string recordingTimeString = GameDatabase.SharedSettings.DateTimeToString( recordingTime );
		string matchPath = GameDatabase.SharedSettings.GetPath<MatchData>( recordingTimeString );

		Directory.CreateDirectory( matchPath );

		await ObsHandler.SetRecordDirectoryAsync( matchPath );
		await ObsHandler.StartRecordAsync();
		await WaitUntilRecordingState( ObsOutputState.Started );

		var match = await StartCollectingMatchData( recordingTime );

		match.VideoUploads.Add( new VideoUpload()
		{
			VideoType = VideoUrlType.RawVideoLink,
			RecordingType = ResultingRecordingType,
		} );

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

		round.VideoUploads.Add( new VideoUpload()
		{
			VideoType = VideoUrlType.RawVideoLink,
			RecordingType = ResultingRecordingType,
		} );
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
			await ObsHandler.ConnectAsync( new Uri( OBSSettings.WebSocketUri ), OBSSettings.WebSocketPassword );
		}
		catch( Exception )
		{
			SendHUDmessage( "Cannot reach OBS", TextMessagePosition.TopMiddle );
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

	private void OnConnected( Uri uri ) => SendHUDmessage( "Connected to OBS.", TextMessagePosition.TopMiddle );
	private void OnDisconnected( Exception exception ) => SendHUDmessage( "Disconnected from OBS.", TextMessagePosition.TopMiddle );
	private void OnRecordingStateChanged( RecordStateChanged changed ) => RecordingState = changed.OutputState;

}