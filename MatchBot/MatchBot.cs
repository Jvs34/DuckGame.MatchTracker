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
							await turnContext.SendActivity( "*Quack*" );

							//await turnContext.SendActivity( "Sorry I can't seem to understand you" );
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

		private List<PlayerData> GetPlayerDataEntities( ITurnContext turnContext , Dictionary<String , List<string>> entities )
		{
			List<PlayerData> players = new List<PlayerData>();

			GlobalData globalData = gameDatabase.globalData;

			if( entities.TryGetValue( "Player_Name" , out List<String> playerNames ) )
			{
				//first off, any "we" should be ignored
				//"me" and "i" should be turned into the user that sent the message

				//TODO: it would be nice if I was able to use fuzzy search here so I'm gonna keep this TODO here
				foreach( String name in playerNames )
				{
					String playerName = name;
					if( String.Equals( playerName , "we" , StringComparison.CurrentCultureIgnoreCase ) )
					{
						continue;
					}

					//TODO:I'm sure I can find a better way to do this later on, maybe some middleware has this as an option
					//if one of the entities is "i" or "me", the user meant himself, so search for his name instead
					if( String.Equals( playerName , "i" , StringComparison.CurrentCultureIgnoreCase ) || String.Equals( playerName , "me" , StringComparison.CurrentCultureIgnoreCase ) )
					{
						playerName = turnContext.Activity.From.Name;
					}

					//try to find the name of the player
					PlayerData pd = globalData.players.FirstOrDefault( p => {
						return String.Equals( p.nickName , playerName , StringComparison.CurrentCultureIgnoreCase ) ||
											   String.Equals( p.name , playerName , StringComparison.CurrentCultureIgnoreCase );
					} );

					if( pd != null )
					{
						players.Add( pd );
					}
				}


			}


			return players;
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


			//break out early if it's already been found
			if( lastPlayed == null )
			{
				//first try to find the actual player object for this

				GlobalData gd = await gameDatabase.GetGlobalData();

				PlayerData pd = gd.players.FirstOrDefault( p =>
					String.Equals( p.nickName , target , StringComparison.CurrentCultureIgnoreCase ) ||
					String.Equals( p.name , target , StringComparison.CurrentCultureIgnoreCase ) );

				if( pd != null )
				{
					//go through the last round the player came up on the search

					var kv = gameDatabase.roundsData.LastOrDefault(
						x => x.Value.players.Any( p =>
						p.userId == pd.userId ) );

					if( kv.Value != null )
					{
						RoundData roundData = kv.Value;
						lastPlayed = roundData.timeEnded;
					}
				}
			}



			if( lastPlayed == null )
			{
				await turnContext.SendActivity( "There doesn't seem to be anything on record" );
			}
			else
			{
				String fancyTarget = target;
				if( target.Equals( "we" , StringComparison.CurrentCultureIgnoreCase ) || target.Equals( "i" , StringComparison.CurrentCultureIgnoreCase ) )
				{
					fancyTarget = "you";
				}

				await turnContext.SendActivity( $"The last time {fancyTarget} played was on {lastPlayed}" );
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
