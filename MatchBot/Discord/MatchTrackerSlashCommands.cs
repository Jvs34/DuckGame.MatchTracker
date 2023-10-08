using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MatchBot.Utils;
using MatchShared.Databases.Interfaces;
using MatchShared.DataClasses;
using System.Text;

//shut the hell up
#pragma warning disable CA1822 // Mark members as static

namespace MatchBot.Discord;

[SlashCommandGroup( "mt", "Commands for MatchTracker" )]
internal class MatchTrackerSlashCommands : ApplicationCommandModule
{
	private IGameDatabase Database { get; }

	public MatchTrackerSlashCommands( IGameDatabase db )
	{
		Database = db;
	}

	[SlashCommand( "quack", "What does it do?" )]
	public async Task Quack( InteractionContext context ) => await context.CreateResponseAsync( "🦆" );

	[SlashCommand( "lastplayed", "Checks when was the last time someone played" )]
	public async Task LastPlayedCommand( InteractionContext context, [Option( "user", "target, can be empty" )] DiscordUser? target = null )
	{
		await context.CreateResponseAsync( "Looking through the database...", true );

		var response = new StringBuilder();

		if( target is null )
		{
			var lastPlayed = await Database.GetLastTimePlayed( null );
			response.Append( $"The last time a match was recorded was on {Formatter.Timestamp( lastPlayed, TimestampFormat.LongDateTime )}" );
		}
		else
		{
			var foundPlayer = await Database.GetPlayer( target );
			if( foundPlayer != null )
			{
				DateTime lastPlayed = await Database.GetLastTimePlayed( foundPlayer );
				response.Append( $"The last time {foundPlayer.GetName()} played was {Formatter.Timestamp( lastPlayed, TimestampFormat.LongDateTime )}" );
			}
			else
			{
				response.Append( "Could not find player in database." );
			}
		}

		await context.EditResponseAsync( new DiscordWebhookBuilder()
			.WithContent( response.ToString() ) );
	}

	[SlashCommand( "timesplayed", "Checks when was the last time someone played" )]
	public async Task TimesPlayedCommand( InteractionContext context,
		[Option( "matchOrRound", "Whether we're doing this for a match(true) or a round(false)" )] bool matchOrRound = true,
		[Option( "user", "target, can be empty" )] DiscordUser? target = null )
	{
		await context.CreateResponseAsync( "Looking through the database...", true );

		var response = new StringBuilder();

		if( target is null )
		{
			var playerStats = await Database.GetTimesPlayed( null, matchOrRound );

			response.Append( $"We played {playerStats.TimesPlayed} {( matchOrRound ? "matches" : "rounds" )} with a playtime of {playerStats.DurationPlayed.ToString( "c" )} hours" );
		}
		else
		{
			var foundPlayer = await Database.GetPlayer( target );
			if( foundPlayer != null )
			{
				var playerStats = await Database.GetTimesPlayed( foundPlayer, matchOrRound );
				response.Append( $"{foundPlayer.GetName()} played {playerStats.TimesPlayed} {( matchOrRound ? "matches" : "rounds" )} with a playtime of {playerStats.DurationPlayed.ToString( "c" )}" );
			}
			else
			{
				response.Append( $"Sorry, there's nothing on record for {target?.Username}" );
			}
		}

		await context.EditResponseAsync( new DiscordWebhookBuilder()
			.WithContent( response.ToString() ) );
	}

	[SlashCommand( "wins", "Checks how many wins" )]
	public async Task WinsCommand( InteractionContext context,
	[Option( "matchOrRound", "Whether we're doing this for a match(true) or a round(false)" )] bool matchOrRound = true,
	[Option( "user", "target, can be empty" )] DiscordUser? target = null )
	{
		await context.CreateResponseAsync( "Looking through the database...", true );

		var response = new StringBuilder();


		if( target is null )
		{
			PlayerData? mostWinsWinner = null;

			var allWinsAndLosses = await Database.GetAllPlayerWinsAndLosses( matchOrRound );

			var highestStats = allWinsAndLosses.MaxBy( x => x.Value.Wins );

			mostWinsWinner = await Database.GetData<PlayerData>( highestStats.Key ?? string.Empty );

			if( mostWinsWinner != null )
			{
				response.Append( $"{mostWinsWinner.GetName()} won {highestStats.Value.Wins} and lost {highestStats.Value.Losses} {( matchOrRound ? "matches" : "rounds" )}" );
			}
			else
			{
				response.Append( "Doesn't seem like there's someone with more wins than anybody else" );
			}
		}
		else
		{

			var foundPlayer = await Database.GetPlayer( target );
			if( foundPlayer != null )
			{
				var stats = await Database.GetPlayerWinsAndLosses( foundPlayer, matchOrRound );

				response.Append( $"{foundPlayer.GetName()} won {stats.Wins} and lost {stats.Losses} {( matchOrRound ? "matches" : "rounds" )}" );
			}
			else
			{
				response.Append( $"Sorry, there's nothing on record for {target?.Username}" );
			}

		}

		await context.EditResponseAsync( new DiscordWebhookBuilder()
			.WithContent( response.ToString() ) );
	}
}

#pragma warning restore CA1822 // Mark members as static