using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class ShowHUDTextMessage : BaseMessage
	{
		public override string MessageType { get; set; } = nameof( ShowHUDTextMessage );
		public string Text { get; set; }
	}
}
