using MatchRecorderShared.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorderShared
{
	public sealed class WebsocketHandler : IDisposable
	{
		private bool disposedValue;
		private WebSocket WebSocket { get; }
		private byte [] ByteBuffer { get; } = new byte [4096 * 10];
		private ConcurrentQueue<BaseMessage> MessagesToSend { get; } = new ConcurrentQueue<BaseMessage>();
		public JsonSerializer Serializer { get; } = JsonSerializer.CreateDefault();

		public event Action<BaseMessage> OnReceiveMessage;

		public WebsocketHandler( WebSocket websocket )
		{
			WebSocket = websocket;
		}


		/// <summary>
		/// Queues a message to be sent during the update loop
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public void SendMessage( BaseMessage message )
		{
			MessagesToSend.Enqueue( message );
		}

		/// <summary>
		/// Call this every frame to send and receive websocket messages
		/// </summary>
		/// <returns></returns>
		public async Task<bool> UpdateLoop()
		{
			await SendLoop();
			return await ReceiveLoop();
		}

		private async Task SendLoop()
		{
			while( MessagesToSend.TryDequeue( out var message ) )
			{
				int bytesWritten = 0;
				using( var memStream = new MemoryStream( ByteBuffer , 0 , ByteBuffer.Length , true ) )
				using( var streamWriter = new StreamWriter( memStream ) )
				using( var jsonWriter = new JsonTextWriter( streamWriter ) )
				{
					Serializer.Serialize( jsonWriter , message );
					bytesWritten = (int) memStream.Position;
				}

				var arraySegment = new ArraySegment<byte>( ByteBuffer , 0 , bytesWritten );
				await WebSocket.SendAsync( arraySegment , WebSocketMessageType.Text , true , CancellationToken.None );
			}
		}

		private async Task<bool> ReceiveLoop()
		{
			var arraySegment = new ArraySegment<byte>( ByteBuffer , 0 , ByteBuffer.Length );
			WebSocketReceiveResult result = await WebSocket.ReceiveAsync( arraySegment , CancellationToken.None );

			if( result.MessageType == WebSocketMessageType.Close )
			{
				return false;
			}

			if( result.MessageType == WebSocketMessageType.Text )
			{
				BaseMessage message = null;
				using( var memStream = new MemoryStream( ByteBuffer , 0 , result.Count , false ) )
				//TODO: using( var compressedStream = new GZipStream( memStream , CompressionMode.Decompress ) )
				using( var streamReader = new StreamReader( memStream ) ) //TODO: StreamReader( compressedStream )
				using( var jsonReader = new JsonTextReader( streamReader ) )
				{
					JObject json = await JObject.LoadAsync( jsonReader );
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

							default:
								break;
						}
					}
				}

				if( message != null && OnReceiveMessage != null )
				{
					OnReceiveMessage( message );
				}
			}
			return true;
		}

		private void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					// TODO: dispose managed state (managed objects)
					WebSocket?.Dispose();
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
