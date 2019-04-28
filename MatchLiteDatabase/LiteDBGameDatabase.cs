using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using LiteDB;

namespace MatchTracker
{
	public class LiteDBGameDatabase : IGameDatabase
	{
		public SharedSettings SharedSettings { get; set; } = new SharedSettings();

		public event LoadGlobalDataDelegate LoadGlobalDataDelegate;
		public event LoadMatchDataDelegate LoadMatchDataDelegate;
		public event LoadRoundDataDelegate LoadRoundDataDelegate;
		public event SaveGlobalDataDelegate SaveGlobalDataDelegate;
		public event SaveMatchDataDelegate SaveMatchDataDelegate;
		public event SaveRoundDataDelegate SaveRoundDataDelegate;
		private BsonMapper Mapper { get; } = new BsonMapper();

		private LiteDatabase Database { get; set; }

		public Stream DatabaseStream { get; set; }
		public string FilePath { get; set; }

		public LiteDBGameDatabase()
		{
			OnModelCreating();
		}

		private void OnModelCreating()
		{
			//TODO: figure out how to add data that we don't add manually to the other collections in an automatic way

			//enable the dbrefs when that's over with
			Mapper.Entity<GlobalData>()
				.Id( x => x.Name )
				//.DbRef( x => x.Players )
				;

			Mapper.Entity<MatchData>()
				.Id( x => x.Name )
				//.DbRef( x => x.Players )
				;

			Mapper.Entity<RoundData>()
				.Id( x => x.Name )
				//.DbRef( x => x.Players )
				;

			Mapper.Entity<PlayerData>()
				.Id( x => x.UserId );

			/*
			Mapper.Entity<TeamData>()
				.DbRef( x => x.Players );
			*/
		}

		public async Task<GlobalData> GetGlobalData( bool forceRefresh = false )
		{
			await Task.CompletedTask;
			var collection = Database.GetCollection<GlobalData>().IncludeAll();
			return collection.FindOne( x => x.Name == nameof( GlobalData ) );
		}

		public async Task<MatchData> GetMatchData( string matchName , bool forceRefresh = false )
		{
			await Task.CompletedTask;
			var collection = Database.GetCollection<MatchData>().IncludeAll();
			return collection.FindOne( x => x.Name == matchName );
		}

		public async Task<RoundData> GetRoundData( string roundName , bool forceRefresh = false )
		{
			await Task.CompletedTask;
			var collection = Database.GetCollection<RoundData>().IncludeAll();
			return collection.FindOne( x => x.Name == roundName );
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback )
		{
			List<Task> callbackTasks = new List<Task>();

			IEnumerable<IWinner> allMatchesOrRounds;

			if( matchOrRound )
			{
				allMatchesOrRounds = Database.GetCollection<MatchData>().IncludeAll().FindAll();
			}
			else
			{
				allMatchesOrRounds = Database.GetCollection<RoundData>().IncludeAll().FindAll();
			}

			foreach( var winnerObj in allMatchesOrRounds )
			{
				callbackTasks.Add( callback( winnerObj ) );
			}

			await Task.WhenAll( callbackTasks );
		}

		public async Task Load()
		{
			await Task.CompletedTask;

			if( Database != null )
			{
				return;
			}

			if( string.IsNullOrEmpty( FilePath ) && DatabaseStream == null )
			{
				throw new ArgumentNullException( "Please fill either FilePath or DatabaseStream for the LiteDB database!" );
			}

			Database = new LiteDatabase( FilePath , Mapper );
		}

		public async Task SaveGlobalData( GlobalData globalData )
		{
			await Task.CompletedTask;
			var collection = Database.GetCollection<GlobalData>();
			collection.Upsert( globalData );
		}

		public async Task SaveMatchData( string matchName , MatchData matchData )
		{
			await Task.CompletedTask;
			var collection = Database.GetCollection<MatchData>();
			collection.Upsert( matchData );
		}

		public async Task SaveRoundData( string roundName , RoundData roundData )
		{
			await Task.CompletedTask;
			var collection = Database.GetCollection<RoundData>();
			collection.Upsert( roundData );
		}
	}
}
