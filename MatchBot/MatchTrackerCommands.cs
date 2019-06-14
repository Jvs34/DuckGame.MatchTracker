using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Humanizer;
using Humanizer.Localisation;
using MatchTracker;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

		[Command( "Uploads" )]
		public async Task UploadsLeftCommand( CommandContext ctx )
		{
			var message = await ctx.RespondAsync( "Looking through the database..." );

			await ctx.Channel.TriggerTypingAsync();

			int uploadsLeft = await GetUploadsLeft();

			TimeSpan timeSpan = TimeSpan.FromDays( uploadsLeft / 100f );

			await message.ModifyAsync( $"There are {uploadsLeft} Youtube videos left to upload, taking {timeSpan.Humanize( culture: GetLocale( ctx.Client ) , maxUnit: TimeUnit.Month )} to upload" );
		}

		[Command( "LastPlayed" )]
		public async Task LastPlayedCommand( CommandContext ctx , params DiscordUser [] targets )
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
		public async Task TimesPlayedCommand( CommandContext ctx , bool matchOrRound , params DiscordUser [] targets )
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
		public async Task MostWinsCommand( CommandContext ctx , bool matchOrRound , params DiscordUser [] targets )
		{
			var message = await ctx.RespondAsync( "Looking through the database..." );

			await ctx.Channel.TriggerTypingAsync();

			StringBuilder response = new StringBuilder();

			var globalData = await DB.GetData<GlobalData>();

			if( targets.Length == 0 )
			{
				PlayerData mostWinsWinner = null;
				int mostWins = 0;
				int mostWinsLosses = 0;

				foreach( PlayerData player in globalData.Players )
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

		[Command( "VoteMaps" )]
		public async Task VoteMapsCommand( CommandContext ctx )
		{
			var globalData = await DB.GetData<GlobalData>();

			await VoteMapCommand( ctx , globalData.Levels.ToArray() );
		}

		[Command( "VoteMap" )]
		public async Task VoteMapCommand( CommandContext ctx , params string [] levelIDs )
		{
			await VoteDatabase<LevelData>( ctx , levelIDs );
		}

		[Command( "VoteRounds" )]
		public async Task VoteRoundsCommand( CommandContext ctx , string matchName )
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
		public async Task VoteRoundCommand( CommandContext ctx , params string [] databaseIndexes )
		{
			await VoteDatabase<RoundData>( ctx , databaseIndexes );
		}

		private async Task VoteDatabase<T>( CommandContext ctx , params string [] databaseIndexes ) where T : IDatabaseEntry, ITagsList
		{
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

		#endregion

		#region UTILS
		private async Task<DateTime> GetLastTimePlayed( PlayerData player )
		{
			DateTime lastPlayed = DateTime.MinValue;
			object lastPlayedLock = new object();

			await DB.IterateOverAllRoundsOrMatches( true , async ( matchOrRound ) =>
			{
				if( player == null || matchOrRound.Players.Contains( player ) )
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

				if( player == null || matchOrRound.Players.Any( x => x.UserId == player.UserId ) )
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

			GlobalData globalData = await DB.GetData<GlobalData>();

			foundPlayerData = globalData.Players.Find( p => p.DiscordId == discordUser.Id || p.DatabaseIndex == discordUser.Id.ToString() );

			if( foundPlayerData == null )
			{
				foundPlayerData = globalData.Players.Find( p =>
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
				return true;
			} );

			return (wins, losses);
		}
		#endregion
	}
}
