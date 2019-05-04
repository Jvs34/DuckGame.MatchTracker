using MatchTracker;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MatchBot
{
	public class MatchBot : IBot
	{
		private enum GameType
		{
			Match,
			Round
		}

		private enum TargetType
		{
			Everyone, //targets everyone
			Author, //targets the author of the message
			Other //targets someone else
		}

		//class used to store info about the recognized Luis entity
		private class RecognizedPlayerData
		{
			public string FancyTarget { get; set; }
			public bool IsSpecialTarget { get; set; }

			//if this target is a special one, it means the target and what the PlayerDataTarget might be set by code instead of a normal name search
			public PlayerData PlayerDataTarget { get; set; }

			public string Target { get; set; } //"we" , "i" , "me" , "Jvs" these are the kind of targets we can expect

			//the target of this, might be null if not found
			public TargetType TargetType { get; set; } //the kind of target of this entity

			//the colloquial target
		}



		private readonly HttpClient httpClient;
		private readonly Timer refreshTimer;
		private Task loadDatabaseTask;
		private IConfigurationRoot Configuration { get; }
		private JsonSerializerSettings JsonSettings { get; }
		private BotSettings botSettings;

		private LuisRecognizer Recognizer { get; }

		private HttpGameDatabase remoteGameDatabase;
		private FileSystemGameDatabase localGameDatabase;

		private GameDatabase Database
		{
			get
			{
				return botSettings.UseRemoteDatabase ? (GameDatabase) remoteGameDatabase : localGameDatabase;
			}
		}

		public MatchBot( IConfigurationRoot configuration )
		{
			botSettings = new BotSettings();
			Configuration = configuration;

			JsonSettings = new JsonSerializerSettings()
			{
				PreserveReferencesHandling = PreserveReferencesHandling.Objects ,
			};

			httpClient = new HttpClient( new SocketsHttpHandler()
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate ,
				MaxConnectionsPerServer = 50 , //this is not set to a good limit by default, which fucks up my connection apparently
				ConnectTimeout = TimeSpan.FromMinutes( 30 )
			} )
			{
				Timeout = TimeSpan.FromMinutes( 30 )
			};

			remoteGameDatabase = new HttpGameDatabase( httpClient );
			localGameDatabase = new FileSystemGameDatabase();

			Configuration.Bind( remoteGameDatabase.SharedSettings );
			Configuration.Bind( botSettings );

			localGameDatabase.SharedSettings = remoteGameDatabase.SharedSettings;



			LuisApplication luisApplication = new LuisApplication( botSettings.LuisModelId , botSettings.LuisSubcriptionKey , botSettings.LuisUri.ToString() );

			Recognizer = new LuisRecognizer( luisApplication ,
				new LuisPredictionOptions()
				{
					IncludeAllIntents = true
				}
			);

			RefreshDatabase();
			refreshTimer = new Timer( RefreshDatabase , null , TimeSpan.Zero , TimeSpan.FromHours( 1 ) );
		}

		public async Task OnTurnAsync( ITurnContext turnContext , CancellationToken cancellationToken )
		{
			await loadDatabaseTask;

			if( turnContext.Activity.Type == ActivityTypes.Message )
			{

				Console.WriteLine( turnContext.Activity.Text );


				var result = await Recognizer.RecognizeAsync( turnContext , cancellationToken );
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
					case "Upload":
						{
							await HandleUploadsLeft( turnContext , result );
							break;
						}
					case "Help":
						{
							await HandleHelp( turnContext );
							break;
						}
					default:
						{
							await turnContext.SendActivityAsync( "*Quack*" );
							break;
						}
				}
			}
		}

		private async Task HandleUploadsLeft( ITurnContext turnContext , RecognizerResult result )
		{
			await turnContext.SendActivityAsync( "cba implementing right now" );
		}

		public void RefreshDatabase( object dontactuallycare = null )
		{
			if( loadDatabaseTask?.IsCompleted == false )
			{
				Console.WriteLine( "Database hasn't finished loading, skipping refresh" );
				return;
			}

			loadDatabaseTask = Database.Load();
		}

		private Dictionary<string , List<string>> GetEntities( IDictionary<string , JToken> results )
		{
			Dictionary<string , List<string>> list = new Dictionary<string , List<string>>();
			foreach( var it in results )
			{
				if( it.Key == "$instance" )
					continue;
				list.TryAdd( it.Key , it.Value.ToObject<List<string>>() );
			}
			return list;
		}

		private async Task<List<RecognizedPlayerData>> GetPlayerDataEntities( ITurnContext turnContext , Dictionary<string , List<string>> entities )
		{
			List<RecognizedPlayerData> playerTargets = new List<RecognizedPlayerData>();
			//List<PlayerData> players = new List<PlayerData>();

			GlobalData globalData = await Database.GetGlobalData();

			if( entities.TryGetValue( "Player_Name" , out List<string> playerNames ) )
			{
				//"me" and "i" should be turned into the user that sent the message

				//TODO: it would be nice if I was able to use fuzzy search here so I'm gonna keep this TODO here
				foreach( string name in playerNames )
				{
					RecognizedPlayerData recognizedPlayerData = new RecognizedPlayerData();
					string playerName = name;
					//if there's a "we"

					if( string.Equals( playerName , "we" , StringComparison.CurrentCultureIgnoreCase ) )
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
						if( string.Equals( playerName , "i" , StringComparison.CurrentCultureIgnoreCase )
							|| string.Equals( playerName , "me" , StringComparison.CurrentCultureIgnoreCase ) )
						{
							playerName = turnContext.Activity.From.Name;
							recognizedPlayerData.IsSpecialTarget = true;
							recognizedPlayerData.TargetType = TargetType.Author;
							recognizedPlayerData.FancyTarget = "you";
						}

						//try to find the name of the player
						PlayerData pd = globalData.Players.Find( p =>
						{
							return string.Equals( p.NickName , playerName , StringComparison.CurrentCultureIgnoreCase )
												   || string.Equals( p.Name , playerName , StringComparison.CurrentCultureIgnoreCase );
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

		private async Task<(int, int)> GetPlayerWinsAndLosses( PlayerData player , bool ismatchOrRound )
		{
			int wins = 0;
			int losses = 0;

			await Database.IterateOverAllRoundsOrMatches( ismatchOrRound , async ( matchOrRound ) =>
			{
				//even if it's team mode we consider it a win
				//first off, only do this if the play is actually in the match
				if( matchOrRound.Players.Any( x => x.UserId == player.UserId ) )
				{
					List<PlayerData> matchOrRoundWinners = matchOrRound.GetWinners();

					if( matchOrRoundWinners.Any( x => x.UserId == player.UserId ) )
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

		private async Task HandleHelp( ITurnContext turnContext )
		{
			await turnContext.SendActivityAsync( "You can ask me stuff like who won the most, last played or times played" );
			await Task.CompletedTask;
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
					await Database.IterateOverAllRoundsOrMatches( true , async ( matchOrRound ) =>
					{
						IStartEnd startEnd = (IStartEnd) matchOrRound;
						if( startEnd.TimeEnded > lastPlayed )
						{
							lock( lastPlayedLock )
							{
								lastPlayed = startEnd.TimeEnded;
							}
						}
						await Task.CompletedTask;
					} );
				}
				else if( recognizedPlayer.PlayerDataTarget != null )
				{
					//go through the last round the player came up on the search

					await Database.IterateOverAllRoundsOrMatches( true , async ( matchOrRound ) =>
					{
						if( matchOrRound.Players.Any( p => p.UserId == recognizedPlayer.PlayerDataTarget.UserId ) )
						{
							IStartEnd startEnd = (IStartEnd) matchOrRound;
							if( startEnd.TimeEnded > lastPlayed )
							{
								lock( lastPlayedLock )
								{
									lastPlayed = startEnd.TimeEnded;
								}
							}
						}
						await Task.CompletedTask;
					} );
				}

				if( lastPlayed != DateTime.MinValue )
				{
					CultureInfo ci = CultureInfo.CreateSpecificCulture( "en-US" );
					await turnContext.SendActivityAsync( $"The last time {recognizedPlayer.FancyTarget} played was on {lastPlayed.ToString( "HH:mm:ss dddd d MMMM yyyy" , ci )}" );
				}
				else
				{
					await turnContext.SendActivityAsync( $"Sorry, there's nothing on record for {recognizedPlayer.FancyTarget}" );
				}
			}
		}

		private async Task HandleMostWins( ITurnContext turnContext , RecognizerResult result )
		{
			var entities = GetEntities( result.Entities );
			List<RecognizedPlayerData> recognizedPlayerEntities = await GetPlayerDataEntities( turnContext , entities );
			GameType gameType = entities.ContainsKey( "Round" ) ? GameType.Round : GameType.Match;
			string gameTypeString = gameType == GameType.Match ? "matches" : "rounds";

			GlobalData globalData = await Database.GetGlobalData();

			//if there's 0 recognized player objects that means that the user wants to know who won the most, hopefully
			if( recognizedPlayerEntities.Count == 0 )
			{
				//go through every player that's defined in the database and check which one has the most wins
				PlayerData mostWinsWinner = null;
				int mostWins = 0;
				int mostWinsLosses = 0;

				foreach( PlayerData player in globalData.Players )
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
					await turnContext.SendActivityAsync( $"{mostWinsWinner.GetName()} won {mostWins} and lost {mostWinsLosses} {gameTypeString}" );
				}
				else
				{
					await turnContext.SendActivityAsync( "Doesn't seem like there's someone with more wins than anybody else" );
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

						await turnContext.SendActivityAsync( $"{recognizedPlayer.PlayerDataTarget.GetName()} won {wins} and lost {losses} {gameTypeString}" );
					}
					else
					{
						await turnContext.SendActivityAsync( $"Sorry, there's nothing on record for {recognizedPlayer.FancyTarget}" );
					}
				}
			}
		}

		private async Task HandleTimesPlayed( ITurnContext turnContext , RecognizerResult result )
		{
			var entities = GetEntities( result.Entities );
			List<RecognizedPlayerData> recognizedPlayerEntities = await GetPlayerDataEntities( turnContext , entities );
			GameType gameType = entities.ContainsKey( "Round" ) ? GameType.Round : GameType.Match;
			string gameTypeString = gameType == GameType.Match ? "matches" : "rounds";

			foreach( RecognizedPlayerData recognizedPlayer in recognizedPlayerEntities )
			{
				int timesPlayed = 0;
				TimeSpan durationPlayed = TimeSpan.Zero;
				object durationPlayedLock = new object();

				if( recognizedPlayer.TargetType == TargetType.Everyone )
				{
					GlobalData gd = await Database.GetGlobalData();
					timesPlayed = gameType == GameType.Match ? gd.Matches.Count : gd.Rounds.Count;

					await Database.IterateOverAllRoundsOrMatches( gameType == GameType.Match , async ( matchOrRound ) =>
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
					GlobalData gd = await Database.GetGlobalData();

					await Database.IterateOverAllRoundsOrMatches( gameType == GameType.Match , async ( matchOrRound ) =>
					{
						if( matchOrRound.Players.Any( x => x.UserId == recognizedPlayer.PlayerDataTarget.UserId ) )
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
					await turnContext.SendActivityAsync( $"{recognizedPlayer.FancyTarget} played {timesPlayed} {gameTypeString} with {Math.Round( durationPlayed.TotalHours )} hours of playtime" );
				}
				else
				{
					await turnContext.SendActivityAsync( $"Sorry, there's nothing on record for {recognizedPlayer.FancyTarget}" );
				}
			}
		}




	}
}