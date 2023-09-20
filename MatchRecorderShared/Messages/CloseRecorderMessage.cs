using MatchRecorderShared.Messages;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class CloseRecorderMessage : BaseMessage
	{
		public override string MessageType { get; set; } = nameof( CloseRecorderMessage );
	}
}
