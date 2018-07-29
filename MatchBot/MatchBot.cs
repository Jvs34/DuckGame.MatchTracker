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
using System.Globalization;
using System.Net.Http;
using Flurl;
using System.Net;
using System.Web;

namespace MatchBot
{
	public class MatchBot : IBot
	{
		private enum TargetType
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

		private enum GameType
		{
			Match,
			Round
		}

		private readonly GameDatabase gameDatabase;

		private Task loadDatabaseTask;

		private readonly Timer refreshTimer;

		private readonly HttpClient httpClient;

		public MatchBot()
		{
			httpClient = new HttpClient( new SocketsHttpHandler()
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate ,
				MaxConnectionsPerServer = 50 , //this is not set to a good limit by default, which fucks up my connection apparently
				ConnectTimeout = TimeSpan.FromMinutes( 30 )
			} )
			{
				Timeout = TimeSpan.FromMinutes( 30 )
			};

			String settingsFolder = Path.Combine( Path.GetFullPath( Directory.GetCurrentDirectory() ) , "Settings" );
			String sharedSettingsPath = Path.Combine( settingsFolder , "shared.json" );

			gameDatabase = new GameDatabase
			{
				sharedSettings = JsonConvert.DeserializeObject<SharedSettings>( File.ReadAllText( sharedSettingsPath ) )
			};

			gameDatabase.LoadGlobalDataDelegate += LoadDatabaseGlobalDataWeb;
			gameDatabase.LoadMatchDataDelegate += LoadDatabaseMatchDataWeb;
			gameDatabase.LoadRoundDataDelegate += LoadDatabaseRoundDataWeb;

			RefreshDatabase();
			refreshTimer = new Timer( RefreshDatabase , null , TimeSpan.Zero , TimeSpan.FromHours( 1 ) );
		}

		public void RefreshDatabase( Object dontactuallycare = null )
		{
			if( loadDatabaseTask?.IsCompleted == false )
			{
				Console.WriteLine( "Database hasn't finished loading, skipping refresh" );
				return;
			}

			loadDatabaseTask = gameDatabase.Load();
		}

		private async Task<GlobalData> LoadDatabaseGlobalDataWeb( GameDatabase gameDatabase , SharedSettings sharedSettings )
		{
			var response = await httpClient.GetStringAsync( sharedSettings.GetGlobalUrl() );
			Console.WriteLine( "Loading GlobalData" );
			return JsonConvert.DeserializeObject<GlobalData>( HttpUtility.HtmlDecode( response ) );
		}

		private async Task<MatchData> LoadDatabaseMatchDataWeb( GameDatabase gameDatabase , SharedSettings sharedSettings , string matchName )
		{
			var response = await httpClient.GetStringAsync( sharedSettings.GetMatchUrl( matchName ) );
			Console.WriteLine( $"Loading MatchData {matchName}" );
			return JsonConvert.DeserializeObject<MatchData>( HttpUtility.HtmlDecode( response ) );
		}

		private async Task<RoundData> LoadDatabaseRoundDataWeb( GameDatabase gameDatabase , SharedSettings sharedSettings , string roundName )
		{
			var response = await httpClient.GetStringAsync( sharedSettings.GetRoundUrl( roundName ) );
			Console.WriteLine( $"Loading RoundData {roundName}" );
			return JsonConvert.DeserializeObject<RoundData>( HttpUtility.HtmlDecode( response ) );
		}

		public async Task OnTurn( ITurnContext turnContext )
		{
			await loadDatabaseTask;

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
					case "Help":
						{
							await HandleHelp( turnContext );
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

		private async Task HandleHelp( ITurnContext turnContext )
		{
			await turnContext.SendActivity( "You can ask me stuff like who won the most, last played or times played" );
			await Task.CompletedTask;
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
						if( String.Equals( playerName , "i" , StringComparison.CurrentCultureIgnoreCase )
							|| String.Equals( playerName , "me" , StringComparison.CurrentCultureIgnoreCase ) )
						{
							playerName = turnContext.Activity.From.Name;
							recognizedPlayerData.IsSpecialTarget = true;
							recognizedPlayerData.TargetType = TargetType.Author;
							recognizedPlayerData.FancyTarget = "you";
						}

						//try to find the name of the player
						PlayerData pd = globalData.players.Find( p =>
						{
							return String.Equals( p.nickName , playerName , StringComparison.CurrentCultureIgnoreCase )
												   || String.Equals( p.name , playerName , StringComparison.CurrentCultureIgnoreCase );
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
				DateTime lastPlayed = DateTime.MinValue;
				object lastPlayedLock = new object();

				if( recognizedPlayer.TargetType == TargetType.Everyone )
				{
					await gameDatabase.IterateOverAllRoundsOrMatches( true , async ( matchOrRound ) =>
					{

						IStartEnd startEnd = (IStartEnd) matchOrRound;
						if( startEnd.timeEnded > lastPlayed )
						{
							lock( lastPlayedLock )
							{
								lastPlayed = startEnd.timeEnded;
							}
						}
						await Task.CompletedTask;
					} );
				}
				else if( recognizedPlayer.PlayerDataTarget != null )
				{
					//go through the last round the player came up on the search

					await gameDatabase.IterateOverAllRoundsOrMatches( true , async ( matchOrRound ) =>
					{
						if( matchOrRound.players.Any( p => p.userId == recognizedPlayer.PlayerDataTarget.userId ) )
						{
							IStartEnd startEnd = (IStartEnd) matchOrRound;
							if( startEnd.timeEnded > lastPlayed )
							{
								lock( lastPlayedLock )
								{
									lastPlayed = startEnd.timeEnded;
								}
							}
						}
						await Task.CompletedTask;
					} );
				}

				if( lastPlayed != DateTime.MinValue )
				{
					CultureInfo ci = CultureInfo.CreateSpecificCulture( "en-US" );
					await turnContext.SendActivity( $"Gay The last time {recognizedPlayer.FancyTarget} played was on {lastPlayed.ToString( "HH:mm:ss dddd d MMMM yyyy" , ci )}" );
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
			List<RecognizedPlayerData> recognizedPlayerEntities = await GetPlayerDataEntities( turnContext , entities );
			GameType gameType = entities.ContainsKey( "Round" ) ? GameType.Round : GameType.Match;
			String gameTypeString = gameType == GameType.Match ? "matches" : "rounds";

			GlobalData globalData = await gameDatabase.GetGlobalData();

			//if there's 0 recognized player objects that means that the user wants to know who won the most, hopefully
			if( recognizedPlayerEntities.Count == 0 )
			{
				//go through every player that's defined in the database and check which one has the most wins
				PlayerData mostWinsWinner = null;
				int mostWins = 0;
				int mostWinsLosses = 0;

				foreach( PlayerData player in globalData.players )
				{
					int wins = 0;
					int losses = 0;

					(wins, losses) = await GetPlayerWinsAndLosses( player , gameType == GameType.Match );

					if( wins > mostWins )
					{
						mostWinsWinner = player;
						mostWins = wins;
						mostWinsLosses = losses;
					}
				}

				if( mostWinsWinner != null )
				{
					await turnContext.SendActivity( $"{mostWinsWinner.GetName()} won {mostWins} and lost {mostWinsLosses} {gameTypeString}" );
				}
				else
				{
					await turnContext.SendActivity( "Doesn't seem like there's someone with more wins than anybody else" );
				}
			}
			else
			{
				foreach( RecognizedPlayerData recognizedPlayer in recognizedPlayerEntities )
				{
					if( recognizedPlayer.PlayerDataTarget != null )
					{
						int wins = 0;
						int losses = 0;

						(wins, losses) = await GetPlayerWinsAndLosses( recognizedPlayer.PlayerDataTarget , gameType == GameType.Match );

						await turnContext.SendActivity( $"{recognizedPlayer.PlayerDataTarget.GetName()} won {wins} and lost {losses} {gameTypeString}" );
					}
					else
					{
						await turnContext.SendActivity( $"Sorry, there's nothing on record for {recognizedPlayer.FancyTarget}" );
					}
				}
			}
		}

		private async Task<(int, int)> GetPlayerWinsAndLosses( PlayerData player , bool ismatchOrRound )
		{
			int wins = 0;
			int losses = 0;

			await gameDatabase.IterateOverAllRoundsOrMatches( ismatchOrRound , async ( matchOrRound ) =>
			{
				//even if it's team mode we consider it a win
				//first off, only do this if the play is actually in the match
				if( matchOrRound.players.Any( x => x.userId == player.userId ) )
				{
					List<PlayerData> matchOrRoundWinners = matchOrRound.GetWinners();

					if( matchOrRoundWinners.Any( x => x.userId == player.userId ) )
					{
						Interlocked.Increment( ref wins );
					}
					else
					{
						Interlocked.Increment( ref losses );
					}
				}
				await Task.CompletedTask;
			} );

			return (wins, losses);
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
				Object durationPlayedLock = new object();

				if( recognizedPlayer.TargetType == TargetType.Everyone )
				{
					GlobalData gd = await gameDatabase.GetGlobalData();
					timesPlayed = gameType == GameType.Match ? gd.matches.Count : gd.rounds.Count;

					await gameDatabase.IterateOverAllRoundsOrMatches( gameType == GameType.Match , async ( matchOrRound ) =>
					{
						if( matchOrRound is IStartEnd duration )
						{
							lock( durationPlayedLock )
							{
								durationPlayed = durationPlayed.Add( duration.GetDuration() );
							}
						}
						await Task.CompletedTask;
					} );
				}
				else if( recognizedPlayer.PlayerDataTarget != null )
				{
					GlobalData gd = await gameDatabase.GetGlobalData();

					await gameDatabase.IterateOverAllRoundsOrMatches( gameType == GameType.Match , async ( matchOrRound ) =>
					{
						if( matchOrRound.players.Any( x => x.userId == recognizedPlayer.PlayerDataTarget.userId ) )
						{
							Interlocked.Increment( ref timesPlayed );
							if( matchOrRound is IStartEnd duration )
							{
								lock( durationPlayedLock )
								{
									durationPlayed = durationPlayed.Add( duration.GetDuration() );
								}
							}
						}
						await Task.CompletedTask;
					} );
				}

				if( timesPlayed > 0 )
				{
					await turnContext.SendActivity( $"{recognizedPlayer.FancyTarget} played {timesPlayed} {gameTypeString} with {Math.Round( durationPlayed.TotalHours )} hours of playtime" );
				}
				else
				{
					await turnContext.SendActivity( $"Sorry, there's nothing on record for {recognizedPlayer.FancyTarget}" );
				}
			}
		}
	}
}
