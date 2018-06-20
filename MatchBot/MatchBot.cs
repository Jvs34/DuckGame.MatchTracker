using System;
using System.Collections.Generic;
using System.Threading;
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
using System.Text;

namespace MatchBot
{
	public class MatchBot : IBot
	{
		enum GameType
		{
			Match,
			Round
		};

		private GameDatabase gameDatabase;

		private Task loadDatabaseTask;

		public MatchBot()
		{
			String settingsFolder = Path.Combine( Path.GetFullPath( Directory.GetCurrentDirectory() ) , "Settings" );
			String sharedSettingsPath = Path.Combine( settingsFolder , "shared.json" );

			gameDatabase = new GameDatabase();
			gameDatabase.sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) );

			//TODO: turn these into http calls instead
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

		//if this is ever hosted on azure, this part would have to be added somewhere else, maybe on OnTurn
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

		private async Task<List<PlayerData>> GetPlayerDataEntities( ITurnContext turnContext , Dictionary<String , List<string>> entities )
		{
			List<PlayerData> players = new List<PlayerData>();

			GlobalData globalData = await gameDatabase.GetGlobalData();

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
					//if one of the entities is "i" or "me", the user meant himself, so
					if( String.Equals( playerName , "i" , StringComparison.CurrentCultureIgnoreCase ) || String.Equals( playerName , "me" , StringComparison.CurrentCultureIgnoreCase ) )
					{
						playerName = turnContext.Activity.From.Name;
					}

					//try to find the name of the player
					PlayerData pd = globalData.players.FirstOrDefault( p =>
					{
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
			List<PlayerData> players = await GetPlayerDataEntities( turnContext , GetEntities( result.Entities ) );
			//we only target one entity, a name, "I" or "we"
			DateTime? lastPlayed = null;

			PlayerData target = null;

			if( players.Count == 0 )
			{
				//find the last match played
				var lastRound = gameDatabase.roundsData.LastOrDefault();
				if( lastRound.Value != null )
				{
					lastPlayed = lastRound.Value.timeEnded;
				}
			}
			else
			{
				//first try to find the actual player object for this

				GlobalData gd = await gameDatabase.GetGlobalData();

				//we only care about the first player really

				PlayerData pd = players.FirstOrDefault();

				if( pd != null )
				{
					target = pd;
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
				String fancyTarget = "you";

				if( target != null )
				{
					fancyTarget = target.GetName();
				}

				await turnContext.SendActivity( $"The last time {fancyTarget} played was on {lastPlayed}" );
			}
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
			var entities = GetEntities( result.Entities );
			List<PlayerData> playerTargets = await GetPlayerDataEntities( turnContext , entities );
			//is the user asking about matches or rounds?
			GameType gameType = entities.ContainsKey( "Round" ) ? GameType.Round : GameType.Match;
			String gameTypeString = gameType == GameType.Match ? "matches" : "rounds";
			//if there's multiple targets, we need to check if all of them are in the same matches when we count

			int timesPlayed = 0;

			//we're counting all matches/rounds, pretty easy
			if( playerTargets.Count == 0 )
			{
				GlobalData gd = await gameDatabase.GetGlobalData();
				timesPlayed = gameType == GameType.Match ? gd.matches.Count : gd.rounds.Count;
			}
			else
			{
				//we need to check if all of the players are in the same matches/rounds when we count
				GlobalData gd = await gameDatabase.GetGlobalData();

				//TODO: now that I've merged things a little bit with the interfaces, this will hopefully be less terrible


				//gameType == GameType.Match
				await IterateOverAllRoundsOrMatches( gameType == GameType.Match , async ( matchOrRound ) =>
				{
					int count = 0;
					foreach( PlayerData playerData in matchOrRound.players )
					{
						if( playerTargets.Any( x => x.userId == playerData.userId ) )
						{
							count++;
						}
					}

					if( count == playerTargets.Count )
					{
						Interlocked.Increment( ref timesPlayed );
					}
				} );

			}

			if( playerTargets.Count == 0 )
			{
				await turnContext.SendActivity( $"You've played {timesPlayed} {gameTypeString}" );
			}
			else
			{
				String plys = string.Join(" and " , from ply in playerTargets select ply.GetName() );
				String together = playerTargets.Count > 1 ? " together" : String.Empty;
				await turnContext.SendActivity( $"{plys} played {timesPlayed} {gameTypeString}{together}" );
			}
		}

		//I'm thinking this should probably be in the gamedatabase or something
		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback )
		{
			if( callback == null )
				return;

			GlobalData globalData = await gameDatabase.GetGlobalData();

			List<String> matchesOrRounds = matchOrRound ? globalData.matches : globalData.rounds;

			List<Task> callbackTasks = new List<Task>();
			foreach( String matchOrRoundName in matchesOrRounds )
			{
				IWinner iterateItem = matchOrRound ?
					await gameDatabase.GetMatchData( matchOrRoundName ) as IWinner :
					await gameDatabase.GetRoundData( matchOrRoundName ) as IWinner;

				callbackTasks.Add( callback( iterateItem ) );
			}


			await Task.WhenAll( callbackTasks );
		}

	}
}
