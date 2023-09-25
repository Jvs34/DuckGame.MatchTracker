using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class CollectObjectDataMessage : BaseMessage
	{
		public override string MessageType { get; set; } = nameof( CollectObjectDataMessage );
		public List<ObjectData> ObjectDataList { get; set; }
	}
}
