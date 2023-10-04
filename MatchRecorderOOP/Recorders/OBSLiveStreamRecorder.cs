using MatchRecorderShared.Enums;
using MatchTracker;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MatchRecorder.Recorders;

internal sealed class OBSLiveStreamRecorder : BaseRecorder
{
	public OBSLiveStreamRecorder( ILogger<BaseRecorder> logger , IGameDatabase db , ModMessageQueue messageQueue ) : base( logger , db , messageQueue )
	{
		ResultingRecordingType = RecordingType.Video;
		RecorderConfigType = RecorderType.OBSLiveStream;
		throw new System.NotImplementedException();
	}

	public override Task Update()
	{
		throw new System.NotImplementedException();
	}

	protected override Task StartRecordingMatchInternal()
	{
		throw new System.NotImplementedException();
	}

	protected override Task StartRecordingRoundInternal()
	{
		throw new System.NotImplementedException();
	}

	protected override Task StopRecordingMatchInternal()
	{
		throw new System.NotImplementedException();
	}

	protected override Task StopRecordingRoundInternal()
	{
		throw new System.NotImplementedException();
	}
}
