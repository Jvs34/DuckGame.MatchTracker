using MatchRecorderShared.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal interface IModToRecorderMessageQueue
	{
		ConcurrentQueue<BaseMessage> SendMessagesQueue { get; }
		ConcurrentQueue<BaseMessage> ReceiveMessagesQueue { get; }
	}

	internal class ModToRecorderMessageQueue : IModToRecorderMessageQueue
	{
		public ConcurrentQueue<BaseMessage> SendMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();
		public ConcurrentQueue<BaseMessage> ReceiveMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();
	}
}
