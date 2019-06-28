using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Humanizer;
using Humanizer.Localisation;
using MatchTracker;
using System;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchBot
{
	public class MatchTrackerCommands : BaseCommandModule
	{
		private IGameDatabase DB { get; }

		public MatchTrackerCommands( IGameDatabase database )
		{
			DB = database;
		}

		#region COMMANDS
		[Command( "Quack" )]
		public async Task QuackCommand( CommandContext ctx ) => await ctx.RespondAsync( "🦆" );

		[Command( "Uploads" ), Description( "How many uploads are left" )]
		public async Task UploadsLeftCommand( CommandContext ctx )
		{
			var message = await ctx.RespondAsync( "Looking through the database..." );

			await ctx.Channel.TriggerTypingAsync();

			int uploadsLeft = await GetUploadsLeft();

			TimeSpan timeSpan = TimeSpan.FromDays( uploadsLeft / 100f );

			await message.ModifyAsync( $"There are {uploadsLeft} Youtube videos left to upload, taking {timeSpan.Humanize( culture: GetLocale( ctx.Client ) , maxUnit: TimeUnit.Month )} to upload" );
		}

		[Command( "LastPlayed" ), Description( "Checks when was the last time someone played" )]
		public async Task LastPlayedCommand( CommandContext ctx , [Description( "Can be left empty" )] params DiscordUser [] targets )
		{
			var message = await ctx.RespondAsync( "Looking through the database..." );

			await ctx.Channel.TriggerTypingAsync();

			StringBuilder response = new StringBuilder();

			if( targets.Length == 0 )
			{
				DateTime lastPlayed = await GetLastTimePlayed( null );
				response.Append( $"The last time we played was {lastPlayed.Humanize( culture: GetLocale( ctx.Client ) ) }" );
			}
			else
			{
				foreach( var target in targets )
				{
					var foundPlayer = await GetPlayer( target );
					if( foundPlayer != null )
					{
						DateTime lastPlayed = await GetLastTimePlayed( foundPlayer );
						response.Append( $"The last time {foundPlayer.GetName()} played was {lastPlayed.Humanize( culture: GetLocale( ctx.Client ) )}" );
						response.AppendLine();
					}
				}
			}

			await message.ModifyAsync( response.ToString() );
		}

		[Command( "TimesPlayed" )]
		public async Task TimesPlayedCommand(
			CommandContext ctx ,
			[Description( "Whether we're doing this for a match(true) or a round(false)" )]  bool matchOrRound ,
			[Description( "Can be left empty" )] params DiscordUser [] targets )
		{
			var message = await ctx.RespondAsync( "Looking through the database..." );

			await ctx.Channel.TriggerTypingAsync();

			StringBuilder response = new StringBuilder();

			if( targets.Length == 0 )
			{
				(int timesPlayed, TimeSpan timeSpanPlayed) = await GetTimesPlayed( null , matchOrRound );
				response.Append( $"We played {timesPlayed} {( matchOrRound ? "matches" : "rounds" )} with a playtime of {timeSpanPlayed.Humanize( culture: GetLocale( ctx.Client ) )}" );
			}
			else
			{
				foreach( var target in targets )
				{
					var foundPlayer = await GetPlayer( target );
					if( foundPlayer != null )
					{
						(int timesPlayed, TimeSpan timeSpanPlayed) = await GetTimesPlayed( foundPlayer , matchOrRound );
						response.Append( $"{foundPlayer.GetName()} played {timesPlayed} {( matchOrRound ? "matches" : "rounds" )} with a playtime of {timeSpanPlayed.Humanize( culture: GetLocale( ctx.Client ) )}" );
					}
					else
					{
						response.Append( $"Sorry, there's nothing on record for {target?.Username}" );
					}
				}
			}

			await message.ModifyAsync( response.ToString() );
		}

		[Command( "Wins" )]
		public async Task MostWinsCommand(
			CommandContext ctx ,
			[Description( "Whether we're doing this for a match(true) or a round(false)" )] bool matchOrRound ,
			[Description( "Can be left empty" )] params DiscordUser [] targets )
		{
			var message = await ctx.RespondAsync( "Looking through the database..." );

			await ctx.Channel.TriggerTypingAsync();

			StringBuilder response = new StringBuilder();

			if( targets.Length == 0 )
			{
				PlayerData mostWinsWinner = null;
				int mostWins = 0;
				int mostWinsLosses = 0;

				foreach( PlayerData player in await DB.GetAllData<PlayerData>() )
				{
					int wins = 0;
					int losses = 0;

					(wins, losses) = await GetPlayerWinsAndLosses( player , matchOrRound );

					if( wins > mostWins )
					{
						mostWinsWinner = player;
						mostWins = wins;
						mostWinsLosses = losses;
					}
				}

				if( mostWinsWinner != null )
				{
					response.Append( $"{mostWinsWinner.GetName()} won {mostWins} and lost {mostWinsLosses} {( matchOrRound ? "matches" : "rounds" )}" );
				}
				else
				{
					response.Append( "Doesn't seem like there's someone with more wins than anybody else" );
				}
			}
			else
			{
				foreach( var target in targets )
				{
					var foundPlayer = await GetPlayer( target );
					if( foundPlayer != null )
					{

						int wins = 0;
						int losses = 0;

						(wins, losses) = await GetPlayerWinsAndLosses( foundPlayer , matchOrRound );

						response.Append( $"{foundPlayer.GetName()} won {wins} and lost {losses} {( matchOrRound ? "matches" : "rounds" )}" );
					}
					else
					{
						response.Append( $"Sorry, there's nothing on record for {target?.Username}" );
					}
				}
			}

			await message.ModifyAsync( response.ToString() );
		}

		[Command( "VoteMaps" ), Description( "Votes all maps registered in the database " )]
		public async Task VoteMapsCommand( CommandContext ctx )
		{
			await VoteMapCommand( ctx , ( await DB.GetAll<LevelData>() ).ToArray() );
		}

		[Command( "VoteMap" )]
		public async Task VoteMapCommand( CommandContext ctx , params string [] levelIDs )
		{
			await VoteDatabase<LevelData>( ctx , levelIDs );
		}

		[Command( "VoteRounds" )]
		public async Task VoteRoundsCommand( CommandContext ctx , [Description( "The match id to vote rounds for" )] string matchName )
		{
			MatchData matchData = await DB.GetData<MatchData>( matchName );
			if( matchData == null )
			{
				await ctx.RespondAsync( $"{matchName} is not a valid MatchData database index!" );
				return;
			}

			await VoteRoundCommand( ctx , matchData.Rounds.ToArray() );
		}

		[Command( "VoteRound" )]
		public async Task VoteRoundCommand( CommandContext ctx , [Description( "List of rounds to vote for, minimum 1" )] params string [] databaseIndexes )
		{
			await VoteDatabase<RoundData>( ctx , databaseIndexes );
		}

		/*
		[Command( "AddEmojis" )]
		public async Task AddEmojisCommand( CommandContext ctx )
		{

			var globalData = await DB.GetData<GlobalData>();

			//UnicodeEmojis

			var UnicodeEmojisProp = typeof( DiscordEmoji ).GetProperty( "UnicodeEmojis" , System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic );

			IReadOnlyDictionary<string , string> unicodeEmojis = (IReadOnlyDictionary<string, string>) UnicodeEmojisProp.GetValue( null );


			try
			{
				foreach( var unicodeKV in unicodeEmojis )
				{
					await DB.AddTag( unicodeKV.Value , unicodeKV.Key );
				}
			}
			catch( Exception e )
			{

			}
		}
		*/

		#endregion

		#region UTILS
		private async Task<bool> CheckReadOnly( CommandContext ctx )
		{
			if( DB.ReadOnly )
			{
				await ctx.RespondAsync( $"{DB.GetType()} is in readonly mode" );
				return true;
			}

			return false;
		}

		private async Task VoteDatabase<T>( CommandContext ctx , params string [] databaseIndexes ) where T : IDatabaseEntry, ITagsList
		{
			if( await CheckReadOnly( ctx ) )
			{
				return;
			}

			if( ctx.Channel.IsPrivate )
			{
				await ctx.RespondAsync( "Sorry but DSharpPlus's reaction shit doesn't seem to work in DMs, use a proper channel please" );
				return;
			}

			var message = await ctx.RespondAsync( "Looking through the database..." );

			await ctx.Channel.TriggerTypingAsync();

			var paginator = new DatabaseVotePaginator<T>( ctx.Client , DB , databaseIndexes , ctx.User , message );

			var page = await paginator.GetPageAsync();

			await message.ModifyAsync( page.Content , page.Embed );

			await ctx.Client.GetInteractivity().WaitForCustomPaginationAsync( paginator );

			await message.DeleteAsync();
		}

		private async Task<DateTime> GetLastTimePlayed( PlayerData player )
		{
			DateTime lastPlayed = DateTime.MinValue;
			object lastPlayedLock = new object();

			await DB.IterateOverAllRoundsOrMatches( true , async ( matchOrRound ) =>
			{
				if( player == null || matchOrRound.Players.Contains( player.DatabaseIndex ) )
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
				}
				return true;
			} );

			return lastPlayed;
		}

		private async Task<(int, TimeSpan)> GetTimesPlayed( PlayerData player , bool findmatchOrRound )
		{
			int timesPlayed = 0;
			TimeSpan durationPlayed = TimeSpan.Zero;
			object durationPlayedLock = new object();

			await DB.IterateOverAllRoundsOrMatches( findmatchOrRound , async ( matchOrRound ) =>
			{
				await Task.CompletedTask;

				if( player == null || matchOrRound.Players.Contains( player.UserId ) )
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
				return true;

			} );

			return (timesPlayed, durationPlayed);
		}

		private CultureInfo GetLocale( DiscordClient client ) => CultureInfo.GetCultureInfo( client.CurrentUser.Locale );

		private async Task<PlayerData> GetPlayer( DiscordUser discordUser )
		{
			PlayerData foundPlayerData = null;

			var players = await DB.GetAllData<PlayerData>();

			foundPlayerData = players.Find( p => p.DiscordId == discordUser.Id || p.DatabaseIndex == discordUser.Id.ToString() );

			if( foundPlayerData == null )
			{
				foundPlayerData = players.Find( p =>
				{
					return string.Equals( p.NickName , discordUser.Username , StringComparison.CurrentCultureIgnoreCase )
											|| string.Equals( p.Name , discordUser.Username , StringComparison.CurrentCultureIgnoreCase );
				} );
			}

			return foundPlayerData;
		}

		private async Task<int> GetUploadsLeft()
		{
			int uploads = 0;

			await DB.IterateOverAllRoundsOrMatches( false , async ( round ) =>
			{
				await Task.CompletedTask;

				RoundData roundData = (RoundData) round;

				if( roundData.VideoType == VideoType.VideoLink && string.IsNullOrEmpty( roundData.YoutubeUrl ) )
				{
					Interlocked.Increment( ref uploads );
				}

				return true;
			} );

			return uploads;
		}

		private async Task<(int, int)> GetPlayerWinsAndLosses( PlayerData player , bool ismatchOrRound )
		{
			int wins = 0;
			int losses = 0;

			await DB.IterateOverAllRoundsOrMatches( ismatchOrRound , async ( matchOrRound ) =>
			{
				//even if it's team mode we consider it a win
				//first off, only do this if the play is actually in the match
				if( matchOrRound.Players.Contains( player.DatabaseIndex ) )
				{
					if( matchOrRound.GetWinners().Contains( player.DatabaseIndex ) )
					{
						Interlocked.Increment( ref wins );
					}
					else
					{
						Interlocked.Increment( ref losses );
					}
				}
				await Task.CompletedTask;
				return true;
			} );

			return (wins, losses);
		}
		#endregion
	}
}
