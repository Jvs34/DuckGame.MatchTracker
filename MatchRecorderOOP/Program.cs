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

namespace MatchRecorder
{
	class Program
	{
		static async Task Main( string [] args )
		{
#if DEBUG
			Debugger.Launch();
#endif
			using var recorderHandler = new MatchRecorderServer( Directory.GetCurrentDirectory() );

			await recorderHandler.RunAsync( CancellationToken.None );

			Console.WriteLine( "Finished running program" );
			Console.ReadLine();
		}

		static void OnReceiveMessage( BaseMessage message )
		{
			Console.WriteLine( $"Receive message of type {message.GetType()}" );
		}

	}
}
