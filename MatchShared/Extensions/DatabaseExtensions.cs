using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public static class DatabaseExtensions
	{
		public static async Task<Dictionary<string , Dictionary<string , IDatabaseEntry>>> GetBackup( this IGameDatabase db )
		{
			var mainCollection = new Dictionary<string , Dictionary<string , IDatabaseEntry>>();

			mainCollection [nameof( RoundData )] = new Dictionary<string , IDatabaseEntry>();
			foreach( var roundName in await db.GetAll<RoundData>() )
			{
				mainCollection [nameof( RoundData )] [roundName] = await db.GetData<RoundData>( roundName );
			}

			mainCollection [nameof( MatchData )] = new Dictionary<string , IDatabaseEntry>();
			foreach( var matchName in await db.GetAll<MatchData>() )
			{
				mainCollection [nameof( MatchData )] [matchName] = await db.GetData<MatchData>( matchName );
			}

			mainCollection [nameof( LevelData )] = new Dictionary<string , IDatabaseEntry>();
			foreach( var levelName in await db.GetAll<LevelData>() )
			{
				mainCollection [nameof( LevelData )] [levelName] = await db.GetData<LevelData>( levelName );
			}

			mainCollection [nameof( TagData )] = new Dictionary<string , IDatabaseEntry>();
			foreach( var tagName in await db.GetAll<TagData>() )
			{
				mainCollection [nameof( TagData )] [tagName] = await db.GetData<TagData>( tagName );
			}

			mainCollection [nameof( PlayerData )] = new Dictionary<string , IDatabaseEntry>();
			foreach( var playerName in await db.GetAll<PlayerData>() )
			{
				mainCollection [nameof( PlayerData )] [playerName] = await db.GetData<PlayerData>( playerName );
			}

			mainCollection [nameof( EntryListData )] = new Dictionary<string , IDatabaseEntry>();
			foreach( var entryName in await db.GetAll<EntryListData>() )
			{
				mainCollection [nameof( EntryListData )] [entryName] = await db.GetData<EntryListData>( entryName );
			}

			return mainCollection;
		}

		public static async Task IterateOverAllRoundsOrMatches( this IGameDatabase db , bool matchOrRound , Func<IWinner , Task<bool>> callback )
		{
			if( callback == null )
				return;

			List<Task> tasks = new List<Task>();

			var tokenSource = new CancellationTokenSource();

			foreach( string matchOrRoundName in matchOrRound ? await db.GetAll<MatchData>() : await db.GetAll<RoundData>() )
			{
				tasks.Add( IteratorTask( db , matchOrRound , callback , tasks , tokenSource , matchOrRoundName ) );
			}

			await Task.WhenAll( tasks );
		}

		private static async Task IteratorTask( IGameDatabase db , bool matchOrRound , Func<IWinner , Task<bool>> callback , List<Task> tasks , CancellationTokenSource tokenSource , string matchOrRoundName )
		{
			if( tokenSource.IsCancellationRequested )
			{
				return;
			}

			IWinner iterateItem = matchOrRound ?
				await db.GetData<MatchData>( matchOrRoundName ) as IWinner :
				await db.GetData<RoundData>( matchOrRoundName ) as IWinner;

			if( !await callback( iterateItem ) )
			{
				tokenSource.Cancel();

				//immediately clear the tasks list so we don't await anything for no reason anymore
				//the tasks may still run but they won't get any further than the cancellation request check
				tasks.Clear();
			}
		}

		public static async Task AddTag( this IGameDatabase db , string unicode , string fancyName , ITagsList tagsList = null )
		{
			string emojiDatabaseIndex = string.Join( " " , Encoding.UTF8.GetBytes( unicode ) );

			//now check if we exist
			TagData tagData = await db.GetData<TagData>( emojiDatabaseIndex );

			if( tagData == null )
			{
				tagData = new TagData()
				{
					Name = emojiDatabaseIndex ,
					Emoji = unicode ,
					FancyName = fancyName ,
				};

				await db.SaveData( tagData );
			}

			await db.Add( tagData );

			if( tagsList?.Tags.Contains( emojiDatabaseIndex ) == false )
			{
				tagsList.Tags.Add( emojiDatabaseIndex );
			}
		}

		public static async Task<List<string>> GetAll<T>( this IGameDatabase db ) where T : IDatabaseEntry
		{
			List<string> databaseIndexes = new List<string>();

			var entryListData = await db.GetData<EntryListData>( typeof( T ).Name );
			if( entryListData != null )
			{
				databaseIndexes.AddRange( entryListData.Entries );
			}

			return databaseIndexes;
		}

		/// <summary>
		/// Ideally these two should not be used whatsoever, please deprecate after moving the code over
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="db"></param>
		/// <param name="databaseIndexes"></param>
		/// <returns></returns>
		public static async Task<List<T>> GetAllData<T>( this IGameDatabase db , params string [] databaseIndexes ) where T : IDatabaseEntry
		{
			List<T> dataList = new List<T>();

			foreach( var entryIndex in databaseIndexes )
			{
				dataList.Add( await db.GetData<T>( entryIndex ) );
			}

			return dataList;
		}

		/// <summary>
		/// Ideally these two should not be used whatsoever, please deprecate after moving the code over
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="db"></param>
		/// <returns></returns>
		public static async Task<List<T>> GetAllData<T>( this IGameDatabase db ) where T : IDatabaseEntry
		{
			List<T> dataList = new List<T>();

			var dataEntries = await db.GetAll<T>();

			foreach( var entryIndex in dataEntries )
			{
				dataList.Add( await db.GetData<T>( entryIndex ) );
			}

			return dataList;
		}

		public static async Task Add<T>( this IGameDatabase db , T data ) where T : IDatabaseEntry
		{
			await db.Add<T>( data.DatabaseIndex );
		}

		public static async Task Add<T>( this IGameDatabase db , params string [] databaseIndexes ) where T : IDatabaseEntry
		{
			var entryListData = await db.GetData<EntryListData>( typeof( T ).Name );

			bool doAdd = false;

			if( entryListData == null )
			{
				entryListData = new EntryListData()
				{
					Type = typeof( T ).Name
				};

				//signal that we need to add this to the EntryListData itself
				doAdd = entryListData.Type != entryListData.GetType().Name;
			}

			foreach( var dbEntry in databaseIndexes )
			{
				if( !entryListData.Entries.Contains( dbEntry ) )
				{
					entryListData.Entries.Add( dbEntry );
				}
			}

			if( doAdd )
			{
				await db.Add( entryListData );
			}

			await db.SaveData( entryListData );
		}
	}
}
