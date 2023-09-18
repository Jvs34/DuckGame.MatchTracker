using MatchTracker;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MatchRecorder.Recorders
{
	internal sealed class NoVideoRecorder : BaseRecorder
	{
		public override bool IsRecording => IsRecordingMatch;

		public NoVideoRecorder( ILogger<BaseRecorder> logger , IGameDatabase db , ModMessageQueue messageQueue ) : base( logger , db , messageQueue )
		{
			ResultingRecordingType = RecordingType.None;
		}

		public override Task Update() => Task.CompletedTask;

		protected override async Task StartRecordingMatchInternal()
		{
			var match = await StartCollectingMatchData( DateTime.Now );

			if( match is null )
			{
				return;
			}

			match.VideoUploads.Add( new VideoUpload()
			{
				VideoType = VideoUrlType.None ,
				RecordingType = ResultingRecordingType
			} );

			await GameDatabase.SaveData( match );
		}

		protected override async Task StopRecordingMatchInternal() => await StopCollectingMatchData( DateTime.Now );

		protected override async Task StartRecordingRoundInternal()
		{
			var round = await StartCollectingRoundData( DateTime.Now );

			round.VideoUploads.Add( new VideoUpload()
			{
				VideoType = VideoUrlType.None ,
				RecordingType = ResultingRecordingType
			} );
		}

		protected override async Task StopRecordingRoundInternal() => await StopCollectingRoundData( DateTime.Now );
	}
}
