using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class StartRoundMessage : BaseMessage, IPlayersList
	{
		public override string MessageType { get; set; } = nameof( StartRoundMessage );
		public string Level { get; set; }
		public List<string> Players { get; set; }
	}
}
