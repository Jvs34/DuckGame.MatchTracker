using MatchRecorderShared;
using MatchRecorderShared.Messages;
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
			using var handler = new MessageHandler( true );
			handler.OnReceiveMessage += OnReceiveMessage;

			var loopTask = Task.Run( async () => await handler.ThreadedLoop() );

			while( !loopTask.IsCompleted )
			{
				Console.WriteLine( "Checking for new messages" );
				handler.CheckMessages();

				await Task.Delay( TimeSpan.FromSeconds( 4 ) );
			}

			//using var recorderHandler = new MatchRecorder.MatchRecorderServer( Directory.GetCurrentDirectory() );
			Console.ReadLine();
		}

		static void OnReceiveMessage( BaseMessage message )
		{
			Console.WriteLine( $"Receive message of type {message.GetType()}" );
		}

	}
}
