using MatchRecorderShared;
using MatchRecorderShared.Messages;
using Ninja.WebSockets;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace MatchRecorderOOP
{
	class Program
	{
		static async Task Main( string [] args )
		{
			await WebsocketListen();

			//using var recorderHandler = new MatchRecorder.MatchRecorderServer( Directory.GetCurrentDirectory() );
		}

		static void OnReceiveMessage( BaseMessage message )
		{

		}

		static async Task WebsocketListen()
		{
			var websocketFactory = new WebSocketServerFactory();
			int port = 6969;
			//start a tcp server
			var listener = new TcpListener( IPAddress.Any , port );
			listener.Start();

			//only wait for one connection

			using TcpClient tcpClient = await listener.AcceptTcpClientAsync();
			using var tcpStream = tcpClient.GetStream();

			WebSocketHttpContext context = await websocketFactory.ReadHttpHeaderFromStreamAsync( tcpStream );
			try
			{
				if( context.IsWebSocketRequest )
				{
					WebSocket webSocket = await websocketFactory.AcceptWebSocketAsync( context );
					var handler = new WebsocketHandler( webSocket );
					handler.OnReceiveMessage += OnReceiveMessage;
					bool isconnected = true;

					while( isconnected )
					{
						try
						{
							isconnected = await handler.UpdateLoop();
						}
						catch( Exception e )
						{

						}
					}


				}
			}
			catch( Exception e )
			{

			}
			finally
			{
				tcpClient.Close();
			}

		}
	}
}
