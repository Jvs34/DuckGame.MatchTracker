using HarmonyLib;
using MatchRecorderShared;
using MatchRecorderShared.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static DuckGame.RasterFont;

namespace MatchRecorder
{
	internal sealed class ClientMessageHandler
	{
		public event Action<ClientHUDMessage> OnReceiveMessage;
		private ConcurrentQueue<BaseMessage> SendMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();
		private ConcurrentQueue<ClientHUDMessage> ReceiveMessagesQueue { get; } = new ConcurrentQueue<ClientHUDMessage>();
		private JsonSerializer Serializer { get; } = JsonSerializer.CreateDefault();
		private HttpClient HttpClient { get; }

		public ClientMessageHandler( HttpClient httpClient )
		{
			HttpClient = httpClient;
		}

		public void SendMessage( BaseMessage message )
		{
			SendMessagesQueue.Enqueue( message );
		}

		internal void UpdateMessages()
		{
			while( ReceiveMessagesQueue.TryDequeue( out var message ) )
			{
				OnReceiveMessage?.Invoke( message );
			}
		}

		private async Task SendRecorderMessage( BaseMessage message , CancellationToken token = default )
		{
			var stringBuilder = new StringBuilder( 1024 );
			using var sw = new StringWriter( stringBuilder , CultureInfo.InvariantCulture );
			using var jsonTextWriter = new JsonTextWriter( sw );

			Serializer.Serialize( jsonTextWriter , message );

			using var response = await HttpClient.PostAsync(
				message.MessageType.ToLower() ,
				new StringContent( stringBuilder.ToString() , Encoding.UTF8 , "application/json" ) ,
				token );

			await ParseResponseMessages( response );
		}

		private async Task GetAllPendingClientMessages( CancellationToken token = default )
		{
			using var response = await HttpClient.GetAsync( "/messages" , HttpCompletionOption.ResponseHeadersRead , token );

			if( response == null || !response.IsSuccessStatusCode )
			{
				return;
			}

			await ParseResponseMessages( response );
		}

		private async Task ParseResponseMessages( HttpResponseMessage response )
		{
			using var responseContent = await response.Content.ReadAsStreamAsync();
			using var reader = new StreamReader( responseContent );
			using var jsonReader = new JsonTextReader( reader );

			var clientMessages = Serializer.Deserialize<ClientHUDMessage []>( jsonReader );

			if( clientMessages == null )
			{
				return;
			}

			foreach( var message in clientMessages )
			{
				ReceiveMessagesQueue.Enqueue( message );
			}
		}

		internal async Task ThreadedLoop( CancellationToken token = default )
		{
			while( !token.IsCancellationRequested )
			{
				while( SendMessagesQueue.TryDequeue( out var message ) )
				{
					await SendRecorderMessage( message , token );
				}

				//await GetAllPendingClientMessages();
				await Task.Delay( 100 );
			}
		}


	}
}
