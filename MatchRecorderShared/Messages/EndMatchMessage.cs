using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class EndMatchMessage : BaseMessage
	{
		public override string MessageType { get; set; } = typeof( EndMatchMessage ).Name;
	}
}
