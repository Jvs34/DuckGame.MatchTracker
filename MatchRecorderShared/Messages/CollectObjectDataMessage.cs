using MatchTracker;
using System.Collections.Generic;

namespace MatchRecorderShared.Messages;

public class CollectObjectDataMessage : BaseMessage
{
	public override string MessageType { get; set; } = nameof( CollectObjectDataMessage );
	public List<ObjectData> ObjectDataList { get; set; }
}
