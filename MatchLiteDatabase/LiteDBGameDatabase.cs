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
		private BsonMapper Mapper { get; } = new BsonMapper();

		private LiteDatabase Database { get; set; }

		public Stream DatabaseStream { get; set; }

		public string FilePath
		{
			get
			{
				return SharedSettings.GetDatabasePath();
			}
		}

		public bool UseStream { get; set; }

		public bool ReadOnly => DatabaseStream != null;//unless this logic changes in the future, a litedb using a stream will always be readonly

		public LiteDBGameDatabase()
		{
			OnModelCreating();
		}

		private void OnModelCreating()
		{
			//TODO: figure out how to add data that we don't add manually to the other collections in an automatic way
			//unfortunately, I don't think even entity framework does that for you, so that's tough shit

			//enable the dbrefs when that's over with
			Mapper.Entity<GlobalData>()
				.Id( x => x.DatabaseIndex )
				.DbRef( x => x.Players )
				.DbRef( x => x.Levels )
				.DbRef( x => x.Tags )
				;

			Mapper.Entity<MatchData>()
				.Id( x => x.DatabaseIndex )
				.DbRef( x => x.Players )
				.DbRef( x => x.Tags )
				;

			Mapper.Entity<RoundData>()
				.Id( x => x.DatabaseIndex )
				.DbRef( x => x.Players )
				.DbRef( x => x.Tags )
				;

			Mapper.Entity<PlayerData>()
				.Id( x => x.DatabaseIndex );


			Mapper.Entity<TeamData>()
				.DbRef( x => x.Players );


			Mapper.Entity<TagData>()
				.Id( x => x.DatabaseIndex )
				;

			Mapper.Entity<LevelData>()
				.Id( x => x.DatabaseIndex )
				.DbRef( x => x.Tags )
				;
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback )
		{
			CheckDatabase();
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

			Database = UseStream
				? new LiteDatabase( DatabaseStream , Mapper )
				: new LiteDatabase( FilePath , Mapper );
		}

		private void CheckDatabase()
		{
			if( Database == null )
			{
				throw new NullReferenceException( "Database was not loaded, please call LiteDBGameDatabase.Load first!" );
			}
		}

		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			//LiteDB does not need the data index as each class has been mapped to its own index up above with the BsonMapper
			await Task.CompletedTask;

			CheckDatabase();
			var collection = Database.GetCollection<T>();
			collection.Upsert( data );
		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			await Task.CompletedTask;

			if( string.IsNullOrEmpty( dataId ) )
			{
				dataId = typeof( T ).Name;
			}

			CheckDatabase();
			var collection = Database.GetCollection<T>().IncludeAll();
			return collection.FindById( dataId );
		}
	}
}
