using DSharpPlus.Entities;
using MatchShared.Databases.Extensions;
using MatchShared.Databases.Interfaces;
using MatchShared.DataClasses;
using MatchShared.Interfaces;
using System.Collections.Concurrent;

namespace MatchBot.Utils;

internal class PlayerWinsLosses
{
	public int Wins { get; set; }
	public int Losses { get; set; }
}

internal class PlayerTimePlayed
{
	public int TimesPlayed { get; set; }
	public TimeSpan DurationPlayed { get; set; }
}

internal class PlayerWinsLossesSearch
{
	public PlayerWinsLosses WinsLosses { get; set; } = new PlayerWinsLosses();
	public AsyncLock PlayerLock { get; set; } = new AsyncLock();
}

internal static class CommandUtils
{
	internal static async Task<DateTime> GetLastTimePlayed( this IGameDatabase DB, PlayerData? player )
	{
		DateTime lastPlayed = DateTime.MinValue;
		var asyncLock = new AsyncLock();

		await DB.IterateOverAll<MatchData>( async ( matchData ) =>
		{
			if( player == null || matchData.Players.Contains( player.DatabaseIndex ) )
			{
				using var asyncScope = await asyncLock.AcquireAsync();
				if( matchData.TimeEnded > lastPlayed )
				{
					lastPlayed = matchData.TimeEnded;
				}
			}
			return true;
		} );

		return lastPlayed;
	}

	internal static async Task<PlayerTimePlayed> GetTimesPlayed( this IGameDatabase DB, PlayerData? player, bool findmatchOrRound )
	{
		var playerStats = new PlayerTimePlayed();
		var playerStatsLock = new AsyncLock();

		static async Task<bool> CountTimesPlayed<T>( PlayerData? player, T databaseEntry, PlayerTimePlayed stats, AsyncLock asyncLock ) where T : IWinner, IStartEndTime
		{
			if( player == null || databaseEntry.Players.Contains( player.UserId ) )
			{
				using var asyncScope = await asyncLock.AcquireAsync();
				stats.TimesPlayed++;
				stats.DurationPlayed = stats.DurationPlayed.Add( databaseEntry.GetDuration() );
			}
			return true;
		}

		if( findmatchOrRound )
		{
			await DB.IterateOverAll<MatchData>( async ( matchOrRound ) => await CountTimesPlayed( player, matchOrRound, playerStats, playerStatsLock ) );
		}
		else
		{
			await DB.IterateOverAll<RoundData>( async ( matchOrRound ) => await CountTimesPlayed( player, matchOrRound, playerStats, playerStatsLock ) );
		}

		return playerStats;
	}

	internal static async Task<PlayerData?> GetPlayer( this IGameDatabase DB, DiscordUser discordUser )
	{
		PlayerData? foundPlayerData = null;

		await DB.IterateOverAll<PlayerData>( playerData =>
		{
			if( playerData.DiscordId == discordUser.Id
			|| playerData.DatabaseIndex == discordUser.Id.ToString()
			|| string.Equals( playerData.NickName, discordUser.Username, StringComparison.CurrentCultureIgnoreCase )
			|| string.Equals( playerData.Name, discordUser.Username, StringComparison.CurrentCultureIgnoreCase ) )
			{

				foundPlayerData = playerData;
				return Task.FromResult( false );
			}

			return Task.FromResult( true );
		} );

		return foundPlayerData;
	}

	internal static async Task<int> GetUploadsLeft( this IGameDatabase DB )
	{
		int uploads = 0;
		var uploadsLock = new AsyncLock();

		await DB.IterateOverAll<RoundData>( async roundData =>
		{
			foreach( var upload in roundData.VideoUploads )
			{
				if( upload.IsPending() )
				{
					using var uploadsScope = await uploadsLock.AcquireAsync();
					uploads++;
				}
			}

			return true;
		} );

		return uploads;
	}

	internal static async Task<Dictionary<string, PlayerWinsLosses>> GetAllPlayerWinsAndLosses( this IGameDatabase DB, bool ismatchOrRound )
	{
		var playerWinsAndLosses = new ConcurrentDictionary<string, PlayerWinsLossesSearch>();

		var playerIndexes = await DB.GetAllIndexes<PlayerData>();

		//first, create an entry for all registered players
		foreach( var playerIndex in playerIndexes )
		{
			playerWinsAndLosses[playerIndex] = new PlayerWinsLossesSearch();
		}

		static async Task<bool> CountAllWinsAndLosses<T>( T winnerEntry, ConcurrentDictionary<string, PlayerWinsLossesSearch> statsDict ) where T : IWinner
		{
			var winners = winnerEntry.GetWinners();

			//no winners? either a database dud or everyone died, ignore
			if( winners.Count == 0 )
			{
				return true;
			}

			foreach( var playerId in winnerEntry.Players )
			{
				if( !statsDict.TryGetValue( playerId, out var statsSearch ) )
				{
					continue;
				}

				using var winnerLock = await statsSearch.PlayerLock.AcquireAsync();

				if( winners.Contains( playerId ) )
				{
					statsSearch.WinsLosses.Wins++;
				}
				else
				{
					statsSearch.WinsLosses.Losses++;
				}

			}
			return true;
		}

		if( ismatchOrRound )
		{
			await DB.IterateOverAll<MatchData>( async ( matchOrRound ) => await CountAllWinsAndLosses( matchOrRound, playerWinsAndLosses ) );
		}
		else
		{
			await DB.IterateOverAll<RoundData>( async ( matchOrRound ) => await CountAllWinsAndLosses( matchOrRound, playerWinsAndLosses ) );
		}


		return playerWinsAndLosses.ToDictionary( x => x.Key, y => y.Value.WinsLosses );
	}

	internal static async Task<PlayerWinsLosses> GetPlayerWinsAndLosses( this IGameDatabase DB, PlayerData player, bool ismatchOrRound )
	{
		var playerStats = new PlayerWinsLosses();
		var playerStatsLock = new AsyncLock();

		static async Task<bool> CountWinsAndLosses<T>( PlayerData player, T winnerEntry, PlayerWinsLosses stats, AsyncLock asyncLock ) where T : IWinner
		{
			//even if it's team mode we consider it a win
			//first off, only do this if the play is actually in the match
			if( winnerEntry.Players.Contains( player.DatabaseIndex ) )
			{
				if( winnerEntry.GetWinners().Contains( player.DatabaseIndex ) )
				{
					using var winsScope = await asyncLock.AcquireAsync();
					stats.Wins++;
				}
				else
				{
					using var lossesScope = await asyncLock.AcquireAsync();
					stats.Losses++;
				}
			}
			return true; //continue iterating
		}

		if( ismatchOrRound )
		{
			await DB.IterateOverAll<MatchData>( async ( matchOrRound ) => await CountWinsAndLosses( player, matchOrRound, playerStats, playerStatsLock ) );
		}
		else
		{
			await DB.IterateOverAll<RoundData>( async ( matchOrRound ) => await CountWinsAndLosses( player, matchOrRound, playerStats, playerStatsLock ) );
		}

		return playerStats;
	}
}
