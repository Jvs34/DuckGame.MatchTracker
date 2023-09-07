using MatchRecorderShared;
using MatchRecorderShared.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal sealed class ClientMessageHandler
	{
		//public bool Connected => HubConnection.State == HubConnectionState.Connected;
		//private HubConnection HubConnection { get; }
		private ConcurrentQueue<BaseMessage> SendMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();
		private HttpClient HttpClient { get; }

		//private ConcurrentQueue<BaseMessage> ReceiveMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();

		public ClientMessageHandler( HttpClient httpClient )
		{
			HttpClient = httpClient;
		}

		public void SendMessage( BaseMessage message )
		{
			SendMessagesQueue.Enqueue( message );
		}

		internal async Task ThreadedLoop( CancellationToken token = default )
		{
			while( !token.IsCancellationRequested )
			{
				while( SendMessagesQueue.TryDequeue( out var message ) )
				{
					switch( message )
					{
						case StartMatchMessage smm:
							{
								//await HubConnection.InvokeAsync( nameof( ReceiveStartMatchMessage ) , smm );
								break;
							}
						case EndMatchMessage emm:
							{
								// HubConnection.InvokeAsync( nameof( ReceiveEndMatchMessage ) , emm );
								break;
							}
						case StartRoundMessage srm:
							{
								//await HubConnection.InvokeAsync( nameof( ReceiveStartRoundMessage ) , srm );
								break;
							}
						case EndRoundMessage erm:
							{
								//await HubConnection.InvokeAsync( nameof( ReceiveEndRoundMessage ) , erm );
								break;
							}
						default:
							break;
					}
				}
			}
		}


	}
}
