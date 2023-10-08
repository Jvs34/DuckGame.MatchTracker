using MatchRecorder.Shared.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder;

internal sealed class ClientMessageHandler
{
	public event Action<TextMessage> OnReceiveMessage;
	private ConcurrentQueue<BaseMessage> SendMessagesQueue { get; } = new ConcurrentQueue<BaseMessage>();
	private ConcurrentQueue<TextMessage> ReceiveMessagesQueue { get; } = new ConcurrentQueue<TextMessage>();
	private JsonSerializer Serializer { get; } = new JsonSerializer()
	{
		ContractResolver = new CamelCasePropertyNamesContractResolver()
	};

	private HttpClient HttpClient { get; }
	private CancellationToken StopToken { get; }

	public ClientMessageHandler( HttpClient httpClient, CancellationToken token )
	{
		HttpClient = httpClient;
		StopToken = token;
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

	private async Task SendRecorderMessage( BaseMessage message, CancellationToken token = default )
	{
		var stringBuilder = new StringBuilder( 1024 );
		using var sw = new StringWriter( stringBuilder, CultureInfo.InvariantCulture );
		using var jsonTextWriter = new JsonTextWriter( sw );

		Serializer.Serialize( jsonTextWriter, message );

		using var response = await HttpClient.PostAsync(
			message.MessageType.ToLower(),
			new StringContent( stringBuilder.ToString(), Encoding.UTF8, "application/json" ),
			token );
	}

	private async Task GetAllPendingClientMessages( Stopwatch cooldown = null, CancellationToken token = default )
	{
		if( cooldown != null )
		{
			if( cooldown.Elapsed >= TimeSpan.FromMilliseconds( 500 ) )
			{
				cooldown.Restart();
			}
			else
			{
				return;
			}
		}

		using var response = await HttpClient.GetAsync( "/messages", token );

		if( response == null || !response.IsSuccessStatusCode )
		{
			return;
		}

		await ParseResponseMessages( response );
	}

	private async Task ParseResponseMessages( HttpResponseMessage response )
	{
		var responseString = await response.Content.ReadAsStringAsync();
		using var reader = new StringReader( responseString );

		Console.WriteLine( responseString );

		using var jsonReader = new JsonTextReader( reader );

		var clientMessages = Serializer.Deserialize<List<TextMessage>>( jsonReader );

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
		Stopwatch pendingMessagesCooldown = Stopwatch.StartNew();

		while( !token.IsCancellationRequested )
		{
			while( SendMessagesQueue.TryDequeue( out var message ) )
			{
				await SendRecorderMessage( message, token );
			}

			await GetAllPendingClientMessages( pendingMessagesCooldown );
			await Task.Delay( 50 );
		}
	}
}
