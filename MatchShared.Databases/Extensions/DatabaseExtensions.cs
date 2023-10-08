using MatchShared.Databases.Interfaces;
using MatchShared.DataClasses;
using MatchShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchShared.Databases.Extensions;

public static class DatabaseExtensions
{
	//public static async Task<Dictionary<string , Dictionary<string , IDatabaseEntry>>> GetBackupAllOut( this IGameDatabase db )
	//{
	//	return new Dictionary<string , Dictionary<string , IDatabaseEntry>>()
	//	{
	//		[nameof( EntryListData )] = await db.GetBackup<EntryListData>() ,
	//		[nameof( RoundData )] = await db.GetBackup<RoundData>() ,
	//		[nameof( MatchData )] = await db.GetBackup<MatchData>() ,
	//		[nameof( LevelData )] = await db.GetBackup<LevelData>() ,
	//		[nameof( TagData )] = await db.GetBackup<TagData>() ,
	//		[nameof( PlayerData )] = await db.GetBackup<PlayerData>() ,
	//		[nameof( ObjectData )] = await db.GetBackup<ObjectData>() ,
	//		[nameof( DestroyTypeData )] = await db.GetBackup<DestroyTypeData>() ,
	//	};
	//}

	public static async Task ImportBackup( this IGameDatabase db, Dictionary<string, Dictionary<string, IDatabaseEntry>> backup )
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

	private static async Task IteratorTask<T>( IGameDatabase db, string dataName, Func<T, Task<bool>> callback, List<Task> tasks, CancellationTokenSource tokenSource ) where T : IDatabaseEntry
	{
		if( tokenSource.IsCancellationRequested )
		{
			return;
		}

		T iterateItem = await db.GetData<T>( dataName, tokenSource.Token );

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
	public static async Task IterateOver<T>( this IGameDatabase db, Func<T, Task<bool>> callback, List<string> databaseIndexes ) where T : IDatabaseEntry
	{
		var tasks = new List<Task>();

		var tokenSource = new CancellationTokenSource();

		foreach( string dataName in databaseIndexes )
		{
			tasks.Add( IteratorTask( db, dataName, callback, tasks, tokenSource ) );
		}

		await Task.WhenAll( tasks );
	}

	public static async Task IterateOverAll<T>( this IGameDatabase db, Func<T, Task<bool>> callback ) where T : IDatabaseEntry
	{
		await db.IterateOver( callback, await db.GetAllIndexes<T>() );
	}

	public static async Task AddTag( this IGameDatabase db, string unicode, string fancyName, ITagsList tagsList = null )
	{
		string emojiDatabaseIndex = string.Join( " ", Encoding.UTF8.GetBytes( unicode ) );

		//now check if we exist
		TagData tagData = await db.GetData<TagData>( emojiDatabaseIndex );

		if( tagData == null )
		{
			tagData = new TagData()
			{
				Name = emojiDatabaseIndex,
				Emoji = unicode,
				FancyName = fancyName,
			};

			await db.SaveData( tagData );
		}

		await db.Add( tagData );

		if( tagsList != null && !tagsList.Tags.Contains( emojiDatabaseIndex ) )
		{
			tagsList.Tags.Add( emojiDatabaseIndex );
		}
	}

	/// <summary>
	/// Ideally these two should not be used whatsoever, please deprecate after moving the code over
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="db"></param>
	/// <param name="databaseIndexes"></param>
	[Obsolete]
	public static async Task<List<T>> GetAllData<T>( this IGameDatabase db, List<string> databaseIndexes ) where T : IDatabaseEntry
	{
		var dataList = new List<T>();

		foreach( var entryIndex in databaseIndexes )
		{
			dataList.Add( await db.GetData<T>( entryIndex ) );
		}

		return dataList;
	}
}
