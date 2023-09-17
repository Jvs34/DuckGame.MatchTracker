using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class TrackKillMessage : BaseMessage
	{
		public override string MessageType { get; set; } = nameof( TrackKillMessage );
		public KillData KillData { get; set; }
	}
}
