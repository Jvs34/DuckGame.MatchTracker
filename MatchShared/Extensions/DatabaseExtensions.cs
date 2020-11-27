using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public static class DatabaseExtensions
	{
		public static async Task<Dictionary<string , Dictionary<string , IDatabaseEntry>>> GetBackup( this IGameDatabase db )
		{
			var mainCollection = new Dictionary<string , Dictionary<string , IDatabaseEntry>>
			{
				[nameof( EntryListData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( RoundData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( MatchData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( LevelData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( TagData )] = new Dictionary<string , IDatabaseEntry>() ,
				[nameof( PlayerData )] = new Dictionary<string , IDatabaseEntry>() ,
			};

			foreach( var entryName in await db.GetAll<EntryListData>() )
			{
				mainCollection [nameof( EntryListData )] [entryName] = await db.GetData<EntryListData>( entryName );
			}

			foreach( var entryName in await db.GetAll<RoundData>() )
			{
				mainCollection [nameof( RoundData )] [entryName] = await db.GetData<RoundData>( entryName );
			}

			foreach( var entryName in await db.GetAll<MatchData>() )
			{
				mainCollection [nameof( MatchData )] [entryName] = await db.GetData<MatchData>( entryName );
			}

			foreach( var entryName in await db.GetAll<LevelData>() )
			{
				mainCollection [nameof( LevelData )] [entryName] = await db.GetData<LevelData>( entryName );
			}

			foreach( var entryName in await db.GetAll<TagData>() )
			{
				mainCollection [nameof( TagData )] [entryName] = await db.GetData<TagData>( entryName );
			}

			foreach( var entryName in await db.GetAll<PlayerData>() )
			{
				mainCollection [nameof( PlayerData )] [entryName] = await db.GetData<PlayerData>( entryName );
			}

			return mainCollection;
		}

		public static async Task ImportBackup( this IGameDatabase db , Dictionary<string , Dictionary<string , IDatabaseEntry>> backup )
		{
			foreach( var dataTypeKV in backup )
			{
				var dataTypeName = dataTypeKV.Key;
				var dataTypeValue = dataTypeKV.Value;

				foreach( var dataEntry in dataTypeValue )
				{
					await db.SaveData( dataEntry.Value );
				}
			}
		}

		private static async Task IteratorTask<T>( IGameDatabase db , string dataName , Func<T , Task<bool>> callback , List<Task> tasks , CancellationTokenSource tokenSource ) where T : IDatabaseEntry
		{
			if( tokenSource.IsCancellationRequested )
			{
				return;
			}

			T iterateItem = await db.GetData<T>( dataName );

			if( !await callback( iterateItem ) )
			{
				tokenSource.Cancel();

				//immediately clear the tasks list so we don't await anything for no reason anymore
				//the tasks may still run but they won't get any further than the cancellation request check
				tasks.Clear();
			}
		}

		/// <summary>
		/// Iterate over the specified 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="db"></param>
		/// <param name="callback"></param>
		/// <param name="databaseIndexes"></param>
		/// <returns></returns>
		public static async Task IterateOver<T>( this IGameDatabase db , Func<T , Task<bool>> callback , List<string> databaseIndexes ) where T : IDatabaseEntry
		{
			List<Task> tasks = new List<Task>();

			var tokenSource = new CancellationTokenSource();

			foreach( string dataName in databaseIndexes )
			{
				tasks.Add( IteratorTask( db , dataName , callback , tasks , tokenSource ) );
			}

			await Task.WhenAll( tasks );
		}

		public static async Task IterateOverAll<T>( this IGameDatabase db , Func<T , Task<bool>> callback ) where T : IDatabaseEntry
		{
			await db.IterateOver( callback , await db.GetAll<T>() );
		}

		/// <summary>
		/// Legacy, please use IGameDatabase.IterateOverAll directly
		/// </summary>
		/// <param name="db"></param>
		/// <param name="matchOrRound">true for match, false for round</param>
		/// <param name="callback">The callback, return false to interrupt the iteration</param>
		/// <returns></returns>
		public static async Task IterateOverAllRoundsOrMatches( this IGameDatabase db , bool matchOrRound , Func<IWinner , Task<bool>> callback )
		{
			if( matchOrRound )
			{
				async Task<bool> matchTask( MatchData matchData ) => await callback( matchData );

				await db.IterateOverAll<MatchData>( matchTask );
			}
			else
			{
				async Task<bool> roundTask( RoundData roundData ) => await callback( roundData );

				await db.IterateOverAll<RoundData>( roundTask );
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

			if( tagsList != null && !tagsList.Tags.Contains( emojiDatabaseIndex ) )
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
		public static async Task<List<T>> GetAllData<T>( this IGameDatabase db , List<string> databaseIndexes ) where T : IDatabaseEntry
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
		public static async Task<List<T>> GetAllData<T>( this IGameDatabase db ) where T : IDatabaseEntry
		{
			return await db.GetAllData<T>( await db.GetAll<T>() );
		}

		/// <summary>
		/// <para>Adds this item to EntryListData of this type</para>
		/// <para>
		/// This overload calls back to IGameDatabase.Add( string databaseIndex )
		/// NOTE: this will not save the data itself, call db.SaveData for that
		/// </para>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="db"></param>
		/// <param name="data"></param>
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
