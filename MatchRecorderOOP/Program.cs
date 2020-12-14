using MatchRecorderShared;
using MatchRecorderShared.Messages;
using Ninja.WebSockets;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorderOOP
{
	class Program
	{
		static async Task Main( string [] args )
		{
#if DEBUG
			Debugger.Launch();
#endif
			await WebsocketListen();

			//using var recorderHandler = new MatchRecorder.MatchRecorderServer( Directory.GetCurrentDirectory() );
			Console.ReadLine();
		}

		static void OnReceiveMessage( BaseMessage message )
		{

		}

		static async Task WebSocketThreadedLoop( WebsocketHandler handler , CancellationToken token = default )
		{
			while( !handler.IsClosed && !token.IsCancellationRequested )
			{
				Console.WriteLine( "waiting for threaded websocket" );

				try
				{
					if( !await handler.ThreadedReceiveLoop( token ) )
					{
						break;
					}
				}
				catch( Exception e )
				{

				}
			}
		}

		static async Task WebsocketListen()
		{
			var websocketFactory = new WebSocketServerFactory();
			int port = 6969;
			//start a tcp server
			var listener = new TcpListener( IPAddress.Any , port );
			listener.Start();

			//only wait for one connection

			Console.WriteLine( "Waiting for TCPClient connection first" );
			using TcpClient tcpClient = await listener.AcceptTcpClientAsync();
			using var tcpStream = tcpClient.GetStream();

			WebSocketHttpContext context = await websocketFactory.ReadHttpHeaderFromStreamAsync( tcpStream );
			try
			{
				if( context.IsWebSocketRequest )
				{
					CancellationTokenSource tokenSource = new CancellationTokenSource();

					WebSocket webSocket = await websocketFactory.AcceptWebSocketAsync( context , new WebSocketServerOptions()
					{
						KeepAliveInterval = TimeSpan.FromSeconds( 1 )
					} );

					using var handler = new WebsocketHandler( webSocket );
					handler.OnReceiveMessage += OnReceiveMessage;

					var threadedTask = Task.Run( async () => await WebSocketThreadedLoop( handler , tokenSource.Token ) );

					while( !handler.IsClosed )
					{
						Console.WriteLine( "Looping main thread websocket" );
						await handler.UpdateLoop();
						await Task.Delay( TimeSpan.FromSeconds( 1 ) );
					}

					Console.WriteLine( "Websocket was closed, quitting" );
					tokenSource.Cancel();
					await threadedTask;
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
