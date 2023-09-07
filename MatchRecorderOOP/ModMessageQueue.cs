using MatchRecorderShared.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal class ModMessageQueue
	{
		public ConcurrentQueue<BaseMessage> RecorderMessageQueue { get; } = new();
		public ConcurrentQueue<ClientHUDMessage> ClientMessageQueue { get; } = new();

		public ModMessageQueue()
		{
			for( int i = 0; i < 5; i++ )
			{
				ClientMessageQueue.Enqueue( new ClientHUDMessage()
				{
					Message = $"Test {i}"
				} );
			}
		}

	}
}
