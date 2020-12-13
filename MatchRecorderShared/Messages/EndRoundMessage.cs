using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class EndRoundMessage : BaseMessage
	{
		public override string MessageType { get; set; } = typeof( EndRoundMessage ).Name;
	}
}
