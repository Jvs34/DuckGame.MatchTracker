using MatchRecorderShared.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorderShared
{
	public sealed class WebsocketHandler : IDisposable
	{
		private bool disposedValue;
		public WebSocket WebSocket { get; }
		private byte [] SendByteBuffer { get; } = new byte [4096 * 10];
		private byte [] ReceiveByteBuffer { get; } = new byte [4096 * 10];
		private ConcurrentQueue<BaseMessage> SendMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();
		private ConcurrentQueue<BaseMessage> ReceiveMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();
		public JsonSerializer Serializer { get; } = JsonSerializer.CreateDefault();
		public bool Compress { get; }
		public event Action<BaseMessage> OnReceiveMessage;
		public bool IsClosed => WebSocket.State == WebSocketState.Aborted || WebSocket.State == WebSocketState.Aborted;

		public WebsocketHandler( WebSocket websocket , bool compress = false )
		{
			WebSocket = websocket;
			Compress = compress;
		}


		/// <summary>
		/// Queues a message to be sent during the update loop
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public void SendMessage( BaseMessage message )
		{
			SendMessagesQueue.Enqueue( message );
		}

		/// <summary>
		/// Call this every frame to send and receive websocket messages
		/// </summary>
		/// <returns></returns>
		public async Task UpdateLoop()
		{
			await SendLoop();
			ReceiveLoop();
		}

		private async Task SendLoop()
		{
			while( SendMessagesQueue.TryDequeue( out var message ) )
			{
				int bytesWritten = 0;
				using( var memStream = new MemoryStream( SendByteBuffer , 0 , SendByteBuffer.Length , true ) )
				{
					Stream chosenStream = memStream;

					if( Compress )
					{
						chosenStream = new GZipStream( memStream , CompressionMode.Compress );
					}

					using( var streamWriter = new StreamWriter( chosenStream ) )
					using( var jsonWriter = new JsonTextWriter( streamWriter ) )
					{
						Serializer.Serialize( jsonWriter , message );
						bytesWritten = (int) chosenStream.Position;
					}

					if( Compress )
					{
						chosenStream.Dispose();
					}
				}

				var arraySegment = new ArraySegment<byte>( SendByteBuffer , 0 , bytesWritten );
				await WebSocket.SendAsync( arraySegment , Compress ? WebSocketMessageType.Binary : WebSocketMessageType.Text , true , CancellationToken.None );
			}
		}

		private void ReceiveLoop()
		{
			while( ReceiveMessagesQueue.TryDequeue( out var message ) )
			{
				OnReceiveMessage?.Invoke( message );
			}
		}

		public async Task<bool> ThreadedReceiveLoop( CancellationToken token = default )
		{
			var arraySegment = new ArraySegment<byte>( ReceiveByteBuffer , 0 , ReceiveByteBuffer.Length );
			WebSocketReceiveResult result = await WebSocket.ReceiveAsync( arraySegment , token );

			if( result.MessageType == WebSocketMessageType.Close )
			{
				return false;
			}

			var decompressMessage = result.MessageType == WebSocketMessageType.Binary;

			BaseMessage message = null;
			using( var memStream = new MemoryStream( ReceiveByteBuffer , 0 , result.Count , false ) )
			{
				Stream chosenStream = memStream;

				if( decompressMessage )
				{
					chosenStream = new GZipStream( memStream , CompressionMode.Decompress );
				}

				using( var streamReader = new StreamReader( chosenStream ) )
				using( var jsonReader = new JsonTextReader( streamReader ) )
				{
					//TODO: try to make this less wasteful
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

				if( decompressMessage )
				{
					chosenStream.Dispose();
				}
			}

			if( message != null )
			{
				ReceiveMessagesQueue.Enqueue( message );
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
