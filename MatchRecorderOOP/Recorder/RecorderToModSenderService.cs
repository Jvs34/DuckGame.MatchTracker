using MatchRecorderShared.Messages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal class RecorderToModSenderService : BackgroundService
	{
		private ILogger MyLogger { get; }
		private IModToRecorderMessageQueue MessageQueue { get; }
		private IHubContext<MatchRecorderHub> MatchRecorderHub { get; }

		public RecorderToModSenderService( ILogger<RecorderToModSenderService> logger , IModToRecorderMessageQueue messageQueue , IHubContext<MatchRecorderHub> hub )
		{
			MyLogger = logger;
			MessageQueue = messageQueue;
			MatchRecorderHub = hub;
		}

		protected override async Task ExecuteAsync( CancellationToken token )
		{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
			Task.Factory.StartNew( async () =>
			{
				await SendingLoop( token );
			} , token , TaskCreationOptions.LongRunning , TaskScheduler.Default );
#pragma warning restore CS4014
			await Task.CompletedTask;
		}

		internal async Task SendingLoop( CancellationToken token = default )
		{
			while( !token.IsCancellationRequested )
			{
				while( MessageQueue.SendMessagesQueue.TryDequeue( out var message ) )
				{
					switch( message )
					{
						case StartMatchMessage smm:
							{
								await MatchRecorderHub.Clients.All.SendAsync( "ReceiveStartMatchMessage" , smm , cancellationToken: token );
								break;
							}
						case EndMatchMessage emm:
							{
								await MatchRecorderHub.Clients.All.SendAsync( "ReceiveEndMatchMessage" , emm , cancellationToken: token );
								break;
							}
						case StartRoundMessage srm:
							{
								await MatchRecorderHub.Clients.All.SendAsync( "ReceiveStartRoundMessage" , srm , cancellationToken: token );
								break;
							}
						case EndRoundMessage erm:
							{
								await MatchRecorderHub.Clients.All.SendAsync( "ReceiveEndRoundMessage" , erm , cancellationToken: token );
								break;
							}
						case ShowHUDTextMessage shtm:
							{
								await MatchRecorderHub.Clients.All.SendAsync( "ReceiveShowHUDTextMessage" , shtm , cancellationToken: token );
								break;
							}
						default:
							break;
					}
					await Task.Delay( TimeSpan.FromMilliseconds( 100 ) , token );
				}
			}
		}
	}
}
