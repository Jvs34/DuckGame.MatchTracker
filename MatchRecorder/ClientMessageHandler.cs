using MatchRecorderShared;
using MatchRecorderShared.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
		private ConcurrentQueue<BaseMessage> ReceiveMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();

		public ClientMessageHandler()
		{
			//HubConnection = new HubConnectionBuilder()
			//	.WithUrl( "http://localhost:6969/MatchRecorderHub" )
			//	.WithAutomaticReconnect( new TimeSpan [] { TimeSpan.FromSeconds( 1 ) } )
			//	.Build();

			//HubConnection.On<StartMatchMessage>( nameof( ReceiveStartMatchMessage ) , ReceiveStartMatchMessage );
			//HubConnection.On<EndMatchMessage>( nameof( ReceiveEndMatchMessage ) , ReceiveEndMatchMessage );
			//HubConnection.On<StartRoundMessage>( nameof( ReceiveStartRoundMessage ) , ReceiveStartRoundMessage );
			//HubConnection.On<EndRoundMessage>( nameof( ReceiveEndRoundMessage ) , ReceiveEndRoundMessage );
			//HubConnection.On<ShowHUDTextMessage>( nameof( ReceiveShowHUDTextMessage ) , ReceiveShowHUDTextMessage );
		}

		public async Task ConnectAsync()
		{
			//if( HubConnection.State != HubConnectionState.Connected && HubConnection.State != HubConnectionState.Connecting )
			//{
			//	await HubConnection.StartAsync();
			//}
		}

		public void SendMessage( BaseMessage message )
		{
			SendMessagesQueue.Enqueue( message );
		}

		internal void CheckMessages()
		{
			while( ReceiveMessagesQueue.TryDequeue( out var message ) )
			{
				OnReceiveMessage?.Invoke( message );
			}
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
