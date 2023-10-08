using MatchRecorder.Shared.Enums;
using MatchShared.Databases.Interfaces;
using MatchShared.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace MatchRecorder.OOP.Recorders;

/// <summary>
/// WIP
/// </summary>
internal sealed class OBSLiveStreamRecorder : BaseRecorder
{
	public OBSLiveStreamRecorder( ILogger<BaseRecorder> logger, IGameDatabase db, ModMessageQueue messageQueue ) : base( logger, db, messageQueue )
	{
		ResultingRecordingType = RecordingType.Video;
		RecorderConfigType = RecorderType.OBSLiveStream;
		throw new NotImplementedException( "This is not done, use the other recorders in the meantime." );
	}

	public override Task Update()
	{
		throw new NotImplementedException();
	}

	protected override Task StartRecordingMatchInternal()
	{
		throw new NotImplementedException();
	}

	protected override Task StartRecordingRoundInternal()
	{
		throw new NotImplementedException();
	}

	protected override Task StopRecordingMatchInternal()
	{
		throw new NotImplementedException();
	}

	protected override Task StopRecordingRoundInternal()
	{
		throw new NotImplementedException();
	}
}
