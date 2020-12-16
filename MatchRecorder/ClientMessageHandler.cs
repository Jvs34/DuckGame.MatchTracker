using MatchRecorderShared;
using MatchRecorderShared.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchRecorder
{
	internal sealed class ClientMessageHandler
	{
		public bool Connected => HubConnection.State == HubConnectionState.Connected;
		public event Action<BaseMessage> OnReceiveMessage;
		private HubConnection HubConnection { get; }
		private ConcurrentQueue<BaseMessage> ReceiveMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();

		public ClientMessageHandler()
		{
			HubConnection = new HubConnectionBuilder()
				.WithUrl( "http://localhost:6969/MatchRecorderHub" )
				.WithAutomaticReconnect( new TimeSpan [] { TimeSpan.FromSeconds( 1 ) } )
				.Build();
		}

		public async Task ConnectAsync()
		{
			if( HubConnection.State != HubConnectionState.Connected && HubConnection.State != HubConnectionState.Connecting )
			{
				await HubConnection.StartAsync();
			}
		}

		public void SendMessage( BaseMessage message )
		{

		}
		internal void CheckMessages()
		{

		}

		private async Task OnConnectionClosed( Exception exception )
		{
			Console.WriteLine( exception );
			Debug.WriteLine( exception );
			await Task.Delay( 500 );
			await HubConnection.StartAsync();
		}


		public void OnReceiveMessageInternal( JObject message )
		{

		}


	}
}
