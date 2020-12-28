using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared.Messages
{
	public abstract class BaseMessage
	{
		public virtual string MessageType { get; set; } = nameof( BaseMessage );
	}
}
