using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class StartRoundMessage : BaseMessage
	{
		public override string MessageType { get; set; } = typeof( StartRoundMessage ).Name;
		public string Level { get; set; }
	}
}
