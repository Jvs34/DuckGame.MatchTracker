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
		enum TargetType
		{
			Everyone, //targets everyone
			Author, //targets the author of the message
			Other //targets someone else
		}
		//class used to store info about the recognized Luis entity
		private class RecognizedPlayerData
		{
			public String Target { get; set; } //"we" , "i" , "me" , "Jvs" these are the kind of targets we can expect
			public bool IsSpecialTarget { get; set; } //if this target is a special one, it means the target and what the PlayerDataTarget might be set by code instead of a normal name search
			public PlayerData PlayerDataTarget { get; set; } //the target of this, might be null if not found
			public TargetType TargetType { get; set; } //the kind of target of this entity
			public String FancyTarget { get; set; } //the colloquial target
		}

		enum GameType
		{
			Match,
			Round
		}

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

		private async Task<List<RecognizedPlayerData>> GetPlayerDataEntities( ITurnContext turnContext , Dictionary<String , List<string>> entities )
		{
			List<RecognizedPlayerData> playerTargets = new List<RecognizedPlayerData>();
			//List<PlayerData> players = new List<PlayerData>();

			GlobalData globalData = await gameDatabase.GetGlobalData();

			if( entities.TryGetValue( "Player_Name" , out List<String> playerNames ) )
			{
				//"me" and "i" should be turned into the user that sent the message

				//TODO: it would be nice if I was able to use fuzzy search here so I'm gonna keep this TODO here
				foreach( String name in playerNames )
				{
					RecognizedPlayerData recognizedPlayerData = new RecognizedPlayerData();
					String playerName = name;
					//if there's a "we"

					if( String.Equals( playerName , "we" , StringComparison.CurrentCultureIgnoreCase ) )
					{
						recognizedPlayerData.IsSpecialTarget = true;
						recognizedPlayerData.PlayerDataTarget = null;
						recognizedPlayerData.TargetType = TargetType.Everyone;
						recognizedPlayerData.FancyTarget = "you";
					}

					//TODO:I'm sure I can find a better way to do this later on, maybe some middleware has this as an option
					//if one of the entities is "i" or "me", the user meant himself, so

					//the target is already a special one, skip searching for the name
					if( !recognizedPlayerData.IsSpecialTarget )
					{
						if( String.Equals( playerName , "i" , StringComparison.CurrentCultureIgnoreCase ) ||
							String.Equals( playerName , "me" , StringComparison.CurrentCultureIgnoreCase ) )
						{
							playerName = turnContext.Activity.From.Name;
							recognizedPlayerData.IsSpecialTarget = true;
							recognizedPlayerData.TargetType = TargetType.Author;
							recognizedPlayerData.FancyTarget = "you";
						}


						//try to find the name of the player
						PlayerData pd = globalData.players.Find( p =>
						{
							return String.Equals( p.nickName , playerName , StringComparison.CurrentCultureIgnoreCase ) ||
												   String.Equals( p.name , playerName , StringComparison.CurrentCultureIgnoreCase );
						} );

						recognizedPlayerData.PlayerDataTarget = pd;

						if( !recognizedPlayerData.IsSpecialTarget )
						{
							recognizedPlayerData.FancyTarget = playerName;
							recognizedPlayerData.TargetType = TargetType.Other;
						}
					}

					recognizedPlayerData.Target = playerName;

					playerTargets.Add( recognizedPlayerData );

				}

			}

			return playerTargets;
		}

		private async Task HandleLastPlayed( ITurnContext turnContext , RecognizerResult result )
		{
			List<RecognizedPlayerData> recognizedPlayerEntities = await GetPlayerDataEntities( turnContext , GetEntities( result.Entities ) );

			foreach( RecognizedPlayerData recognizedPlayer in recognizedPlayerEntities )
			{
				//if this recognizedplayerdata has a null playertarget and is a special target then it's probably one that targets everyone
				DateTime? lastPlayed = null;

				if( recognizedPlayer.TargetType == TargetType.Everyone )
				{
					var lastRound = gameDatabase.roundsData.LastOrDefault();
					if( lastRound.Value != null )
					{
						lastPlayed = lastRound.Value.timeEnded;
					}
				}
				else if( recognizedPlayer.PlayerDataTarget != null )
				{
					//go through the last round the player came up on the search

					var kv = gameDatabase.roundsData.LastOrDefault( x => x.Value.players.Any( p => p.userId == recognizedPlayer.PlayerDataTarget.userId ) );

					if( kv.Value != null )
					{
						lastPlayed = kv.Value.timeEnded;
					}

				}

				if( lastPlayed != null )
				{
					await turnContext.SendActivity( $"The last time {recognizedPlayer.FancyTarget} played was on {lastPlayed}" );
				}
				else
				{
					await turnContext.SendActivity( $"Sorry, there's nothing on record for {recognizedPlayer.FancyTarget}" );
				}
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
			List<RecognizedPlayerData> recognizedPlayerEntities = await GetPlayerDataEntities( turnContext , entities );
			GameType gameType = entities.ContainsKey( "Round" ) ? GameType.Round : GameType.Match;
			String gameTypeString = gameType == GameType.Match ? "matches" : "rounds";

			foreach( RecognizedPlayerData recognizedPlayer in recognizedPlayerEntities )
			{
				int timesPlayed = 0;
				TimeSpan durationPlayed = TimeSpan.Zero;

				if ( recognizedPlayer.TargetType == TargetType.Everyone )
				{
					GlobalData gd = await gameDatabase.GetGlobalData();
					timesPlayed = gameType == GameType.Match ? gd.matches.Count : gd.rounds.Count;

					Object locking = new object();

					await IterateOverAllRoundsOrMatches( gameType == GameType.Match , async ( matchOrRound ) =>
					{
						if( matchOrRound is IStartEnd duration )
						{
							lock( locking )
							{
								durationPlayed = durationPlayed.Add( duration.GetDuration() );
							}
						}
					});

				}
				else if( recognizedPlayer.PlayerDataTarget != null )
				{
					GlobalData gd = await gameDatabase.GetGlobalData();
					Object locking = new object();

					await IterateOverAllRoundsOrMatches( gameType == GameType.Match , async ( matchOrRound ) =>
					{
						if( matchOrRound.players.Any( x=> x.userId == recognizedPlayer.PlayerDataTarget.userId ) )
						{
							Interlocked.Increment( ref timesPlayed );
							if( matchOrRound is IStartEnd duration )
							{
								lock( locking )
								{
									durationPlayed = durationPlayed.Add( duration.GetDuration() );
								}
							}
						}
					} );
				}

				if( timesPlayed > 0 )
				{
					await turnContext.SendActivity( $"{recognizedPlayer.FancyTarget} played {timesPlayed} {gameTypeString} with {durationPlayed.Hours} hours" );
				}
				else
				{
					await turnContext.SendActivity( $"Sorry, there's nothing on record for {recognizedPlayer.FancyTarget}" );
				}
			}

			/*
			var entities = GetEntities( result.Entities );
			List<RecognizedPlayerData> playerTargets = await GetPlayerDataEntities( turnContext , entities );
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
				String plys = string.Join( " and " , from ply in playerTargets select ply.GetName() );
				String together = playerTargets.Count > 1 ? " together" : String.Empty;
				await turnContext.SendActivity( $"{plys} played {timesPlayed} {gameTypeString}{together}" );
			}
			*/
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
