using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class PingPongMessage : BaseMessage
	{
		public override string MessageType { get; set; } = nameof( PingPongMessage );
	}
}
