using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Ai.LUIS;
using Microsoft.Bot.Builder.Core.Extensions;
using Microsoft.Bot.Schema;

using System.Linq;
using Newtonsoft.Json.Linq;

using MatchTracker;
using System.IO;
using Newtonsoft.Json;
using Microsoft.Cognitive.LUIS.Models;

namespace MatchBot
{
	public class MatchBot : IBot
	{
		private GameDatabase gameDatabase;

		private Task loadDatabaseTask;

		public MatchBot()
		{
			String settingsFolder = Path.Combine( Path.GetFullPath( Directory.GetCurrentDirectory() ) , "Settings" );
			String sharedSettingsPath = Path.Combine( settingsFolder , "shared.json" );

			gameDatabase = new GameDatabase();
			gameDatabase.sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) ); 
			gameDatabase.LoadGlobalData += LoadDatabaseGlobalData;
			gameDatabase.LoadMatchData += LoadDatabaseMatchData;
			gameDatabase.LoadRoundData += LoadDatabaseRoundData;

			loadDatabaseTask = gameDatabase.Load();
		}

		//TODO: turn these asyncs
		private async Task<GlobalData> LoadDatabaseGlobalData( SharedSettings sharedSettings )
		{
			Console.WriteLine( "Loading GlobalData" );
			return sharedSettings.GetGlobalData();
		}

		private async Task<MatchData> LoadDatabaseMatchData( SharedSettings sharedSettings , string matchName )
		{
			Console.WriteLine( $"Loading MatchData {matchName}" );

			return sharedSettings.GetMatchData( matchName );
		}

		private async Task<RoundData> LoadDatabaseRoundData( SharedSettings sharedSettings , string roundName )
		{
			Console.WriteLine( $"Loading RoundData {roundName}" );

			return sharedSettings.GetRoundData( roundName );
		}


		public async Task Initialize()
		{
			await loadDatabaseTask;
		}

		public async Task OnTurn( ITurnContext turnContext )
		{
			if( turnContext.Activity.Type == ActivityTypes.Message )
			{
				Console.WriteLine( turnContext.Activity.Text );
				var result = turnContext.Services.Get<RecognizerResult>( LuisRecognizerMiddleware.LuisRecognizerResultKey );
				var topIntent = result?.GetTopScoringIntent();
				switch( topIntent?.intent )
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
					case "TimesPlayed":
						{
							await HandleTimesPlayed( turnContext , result );
							break;
						}
					default:
						{
							await turnContext.SendActivity( "Sorry I can't seem to understand you" );
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
			var entities = GetEntities( result.Entities );
			//we only target one entity, a name, "I" or "we"
			String target = "we";
			DateTime ? lastPlayed = null;

			if( entities.ContainsKey( "Player_Name" ) )
			{
				//get the first value
				List<String> nameslist = entities ["Player_Name"];
				target = nameslist.FirstOrDefault();
			}

			Console.WriteLine( $"Target is {target}" );



			if( target.Equals( "we" , StringComparison.CurrentCultureIgnoreCase ) )
			{
				//find the last match played
				var lastRound = gameDatabase.roundsData.LastOrDefault();
				if( lastRound.Value != null )
				{
					lastPlayed = lastRound.Value.timeEnded;
				}
			}

			//turn "i" into a name, like the guy that sent the message
			if( target.Equals( "i" , StringComparison.CurrentCultureIgnoreCase ) )
			{
				//who sent the message?
				var from = turnContext.Activity.From;
				target = from.Name;
			}


				//go through the last round the player came up on the search

			var kv = gameDatabase.roundsData.LastOrDefault(
				x => x.Value.players.Any( p => 
				String.Equals( p.nickName ,  target , StringComparison.CurrentCultureIgnoreCase ) ||
				String.Equals( p.name , target , StringComparison.CurrentCultureIgnoreCase ) )
			);
				
			if( kv.Value != null )
			{
				RoundData roundData = kv.Value;
				lastPlayed = roundData.timeEnded;
			}

			if( lastPlayed == null )
			{
				await turnContext.SendActivity( "There doesn't seem to be anything on record" );
			}
			else
			{
				String fancyTarget = target;
				if( target.Equals( "we" , StringComparison.CurrentCultureIgnoreCase ) || target.Equals( "I" , StringComparison.CurrentCultureIgnoreCase ) )
				{
					fancyTarget = "you";
				}

				await turnContext.SendActivity( $"The last time {target} played was on {lastPlayed}" );
			}
			//await turnContext.SendActivity( "Not yet implemented: LastPlayed" );
		}

		private async Task HandleMostWins( ITurnContext turnContext , RecognizerResult result )
		{
			var entities = GetEntities( result.Entities );

			foreach( var kv in entities )
			{
				Console.WriteLine( kv.Key );
			}

			await turnContext.SendActivity( "Not implemented yet: MostWins" );
		}

		private async Task HandleTimesPlayed( ITurnContext turnContext , RecognizerResult result )
		{
			await turnContext.SendActivity( "Not implemented yet: TimesPlayed" );
		}

		private (String, int) GetMostWins( bool matchOrRound , List<String> players )
		{

			return ("", 0);
		}

	}
}
