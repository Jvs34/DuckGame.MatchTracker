using MatchRecorder.Shared.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder.MessageHandlers;

internal interface IMessageHandler
{
	event Action<BaseMessage> OnReceiveMessage;

	void SendMessage( BaseMessage message );
	Task ThreadedLoop( CancellationToken token = default );
	void UpdateMessages();
}
