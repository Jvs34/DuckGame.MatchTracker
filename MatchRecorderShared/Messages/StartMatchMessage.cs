using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class StartMatchMessage : BaseMessage
	{
		public override string MessageType { get; set; } = typeof( StartMatchMessage ).Name;
	}
}
