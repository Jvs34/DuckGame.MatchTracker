using MatchRecorderShared;
using MatchRecorderShared.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal class MatchRecorderHub : Hub<IMessageHandler>, IMessageHandler
	{
		private ILogger MyLogger { get; }
		private IMessageQueue MessageQueue { get; }

		public MatchRecorderHub( ILogger<MatchRecorderHub> logger , IMessageQueue messageQueue )
		{
			MyLogger = logger;
			MessageQueue = messageQueue;
		}

		public async Task ReceiveStartMatchMessage( StartMatchMessage message )
		{
			MessageQueue.ReceiveMessagesQueue.Enqueue( message );
			MyLogger?.LogInformation( $"Received {message.GetType().Name}" );
			await Task.CompletedTask;
		}

		public async Task ReceiveEndMatchMessage( EndMatchMessage message )
		{
			MessageQueue.ReceiveMessagesQueue.Enqueue( message );
			MyLogger?.LogInformation( $"Received {message.GetType().Name}" );
			await Task.CompletedTask;
		}

		public async Task ReceiveStartRoundMessage( StartRoundMessage message )
		{
			MessageQueue.ReceiveMessagesQueue.Enqueue( message );
			MyLogger?.LogInformation( $"Received {message.GetType().Name}" );
			await Task.CompletedTask;
		}

		public async Task ReceiveEndRoundMessage( EndRoundMessage message )
		{
			MessageQueue.ReceiveMessagesQueue.Enqueue( message );
			MyLogger?.LogInformation( $"Received {message.GetType().Name}" );
			await Task.CompletedTask;
		}

		public async Task ReceiveShowHUDTextMessage( ShowHUDTextMessage message )
		{
			MessageQueue.ReceiveMessagesQueue.Enqueue( message );
			MyLogger?.LogInformation( $"Received {message.GetType().Name}" );
			await Task.CompletedTask;
		}
	}
}