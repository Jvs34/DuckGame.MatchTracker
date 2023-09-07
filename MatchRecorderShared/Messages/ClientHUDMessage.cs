using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class ClientHUDMessage : BaseMessage
	{
		public override string MessageType { get; set; } = nameof( ClientHUDMessage );
		public string Message { get; set; } = string.Empty;
	}
}
