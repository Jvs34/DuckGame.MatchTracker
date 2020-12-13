using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public class BaseMessage
	{
		public virtual string MessageType { get; set; } = typeof( BaseMessage ).Name;
	}
}
