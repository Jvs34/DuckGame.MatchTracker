using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class StartMatchMessage : BaseMessage, IPlayersList
	{
		public override string MessageType { get; set; } = nameof( StartMatchMessage );
		public List<string> Players { get; set; }
	}
}
