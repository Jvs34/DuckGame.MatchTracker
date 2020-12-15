using MatchRecorderShared;
using MatchRecorderShared.Messages;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal class SignalRMessageHandler : IMessageHandler
	{
		public event Action<BaseMessage> OnReceiveMessage;

		public void OnReceiveMessageInternal( JObject message )
		{

		}
	}
}
