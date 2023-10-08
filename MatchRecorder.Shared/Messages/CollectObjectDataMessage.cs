using MatchShared.DataClasses;
using System.Collections.Generic;

namespace MatchRecorder.Shared.Messages;

public class CollectObjectDataMessage : BaseMessage
{
	public override string MessageType { get; set; } = nameof( CollectObjectDataMessage );
	public List<ObjectData> ObjectDataList { get; set; }
}
