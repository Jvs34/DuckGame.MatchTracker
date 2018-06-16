using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Ai.LUIS;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace MatchBot
{
	public class MatchBot : IBot
	{
		public async Task OnTurn( ITurnContext turnContext )
		{
			if( turnContext.Activity.Type == ActivityTypes.Message )
			{
				Console.WriteLine( turnContext.Activity.Text );
				var result = turnContext.Services.Get<RecognizerResult>( LuisRecognizerMiddleware.LuisRecognizerResultKey );
				var topIntent = result?.GetTopScoringIntent();
				switch( ( topIntent != null ) ? topIntent.Value.intent : null )
				{
					case "LastPlayed":
						{
							await HandleLastPlayed( turnContext , result );
							break;
						}
					case "MostWins":
						{
							await HandleMostWins( turnContext , result );
							break;
						}
					default:
						{
							turnContext.SendActivity( "Sorry, I don't understand the message" );
							break;
						}
				}
			}
		}

		private Dictionary<String , List<string>> GetEntities( IDictionary<String , JToken> results )
		{
			Dictionary<String , List<string>> list = new Dictionary<string , List<string>>();
			foreach( var it in results )
			{
				if( it.Key == "$instance" )
					continue;
				list.TryAdd( it.Key , it.Value.ToObject<List<string>>() );
			}
			return list;
		}

		private async Task HandleLastPlayed( ITurnContext turnContext , RecognizerResult result )
		{
			//check if we have a target to the query, if the target is "I", the user that sent the message is the target
			var entities = GetEntities( result.Entities );
			//we only target one player

			await turnContext.SendActivity( "Not implemented yet, LastPlayed" );
		}

		private async Task HandleMostWins( ITurnContext turnContext , RecognizerResult result )
		{
			var entities = GetEntities( result.Entities );

			foreach( var kv in entities )
			{
				Console.WriteLine( kv.Key );
			}

			await turnContext.SendActivity( "Not implemented yet, MostWins" );
		}

		private (String, int) GetMostWins( bool matchOrRound , List<String> players )
		{

			return ("", 0);
		}

	}
}
