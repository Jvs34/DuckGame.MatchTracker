using MatchRecorderShared.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatchRecorder
{
	public interface IMessageQueue
	{
		ConcurrentQueue<BaseMessage> SendMessagesQueue { get; }
		ConcurrentQueue<BaseMessage> ReceiveMessagesQueue { get; }
	}

	public class MessageQueue : IMessageQueue
	{
		public ConcurrentQueue<BaseMessage> SendMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();

		public ConcurrentQueue<BaseMessage> ReceiveMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();
	}
}
