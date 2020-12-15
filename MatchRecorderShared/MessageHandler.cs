using MatchRecorderShared.Messages;
using Newtonsoft.Json.Linq;
using PipeMethodCalls;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorderShared
{
	public sealed class MessageHandler : IMessageHandler, IDisposable
	{
		private bool disposedValue;
		public bool IsServer { get; set; }
		private PipeServerWithCallback<IMessageHandler , IMessageHandler> PipeServer { get; }
		private PipeClientWithCallback<IMessageHandler , IMessageHandler> PipeClient { get; }
		public event Action<BaseMessage> OnReceiveMessage;
		private ConcurrentQueue<BaseMessage> SendMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();
		private ConcurrentQueue<BaseMessage> ReceiveMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();

		public MessageHandler( bool server )
		{
			IsServer = server;

			if( IsServer )
			{
				PipeServer = new PipeServerWithCallback<IMessageHandler , IMessageHandler>( GetType().Name.ToLower() , () => this );
			}
			else
			{
				PipeClient = new PipeClientWithCallback<IMessageHandler , IMessageHandler>( GetType().Name.ToLower() , () => this );
			}
		}

		public void SendMessage( BaseMessage message )
		{
			SendMessagesQueue.Enqueue( message );
		}

		/// <summary>
		/// Call this to check for messages and trigger OnReceiveMessage
		/// </summary>
		public void CheckMessages()
		{
			while( ReceiveMessagesQueue.TryDequeue( out var message ) )
			{
				OnReceiveMessage?.Invoke( message );
			}
		}

		public void OnReceiveMessageInternal( JObject json )
		{
			BaseMessage message = null;

			if( json.TryGetValue( nameof( BaseMessage.MessageType ) , StringComparison.InvariantCultureIgnoreCase , out var value ) && value.Type == JTokenType.String )
			{
				switch( value.ToString() )
				{
					case nameof( StartMatchMessage ):
						{
							message = json.ToObject<StartMatchMessage>();
							break;
						}
					case nameof( EndMatchMessage ):
						{
							message = json.ToObject<EndMatchMessage>();
							break;
						}
					case nameof( StartRoundMessage ):
						{
							message = json.ToObject<StartRoundMessage>();
							break;
						}
					case nameof( EndRoundMessage ):
						{
							message = json.ToObject<EndRoundMessage>();
							break;
						}
					case nameof( ShowHUDTextMessage ):
						{
							message = json.ToObject<ShowHUDTextMessage>();
							break;
						}
					default:
						break;
				}
			}

			if( message != null )
			{
				ReceiveMessagesQueue.Enqueue( message );
			}
		}

		/// <summary>
		/// Call me in a different thread with Task.Run or something
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public async Task ThreadedLoop( CancellationToken token = default )
		{
			if( IsServer )
			{
				await PipeServer.WaitForConnectionAsync( token );
			}
			else
			{
				await PipeClient.ConnectAsync( token );
			}

			while( !token.IsCancellationRequested )
			{
				if( SendMessagesQueue.TryDequeue( out var message ) )
				{
					if( IsServer )
					{
						await PipeServer.InvokeAsync( x => x.OnReceiveMessageInternal( JObject.FromObject( message ) ) , token );
					}
					else
					{
						await PipeClient.InvokeAsync( x => x.OnReceiveMessageInternal( JObject.FromObject( message ) ) , token );
					}
				}
			}

			if( IsServer )
			{
				await PipeServer.WaitForRemotePipeCloseAsync();
			}
			else
			{
				await PipeClient.WaitForRemotePipeCloseAsync();
			}
		}


		private void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					// TODO: dispose managed state (managed objects)
					if( IsServer )
					{
						PipeServer?.Dispose();
					}
					else
					{
						PipeClient.Dispose();
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}


	}
}
