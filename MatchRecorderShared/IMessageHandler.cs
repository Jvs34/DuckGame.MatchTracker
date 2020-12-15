using MatchRecorderShared.Messages;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared
{
	internal interface IMessageHandler
	{
		void OnReceiveMessageInternal( JObject message );
	}
}
