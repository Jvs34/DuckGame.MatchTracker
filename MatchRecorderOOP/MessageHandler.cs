using MatchRecorderShared.Messages;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatchRecorder
{
	public class MessageHandler : Hub
	{
		public event Action<BaseMessage> OnReceiveMessage;

		public void SendMessage( BaseMessage message )
		{

		}

		internal void CheckMessages()
		{

		}
	}
}
