using MatchRecorderShared.Messages;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared
{
	public interface IMessageHandler
	{
		void OnReceiveMessageInternal( JObject message );
		event Action<BaseMessage> OnReceiveMessage;
	}
}
