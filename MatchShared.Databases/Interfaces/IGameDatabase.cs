using MatchShared.Databases.Settings;
using MatchShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MatchShared.Databases.Interfaces;


public interface IGameDatabase : IDisposable
{
	SharedSettings SharedSettings { get; set; }

	bool IsReadOnly { get; }
	bool IsLoaded { get; }

	Task Load( CancellationToken token = default );

	Task<bool> SaveData<T>( T data, CancellationToken token = default ) where T : IDatabaseEntry;
	Task<bool> SaveData<T>( IEnumerable<T> datas, CancellationToken token = default ) where T : IDatabaseEntry;
	Task<bool> DeleteData<T>( T data, CancellationToken token = default ) where T : IDatabaseEntry;
	Task<bool> DeleteData<T>( IEnumerable<T> datas, CancellationToken token = default ) where T : IDatabaseEntry;
	Task<bool> DeleteData<T>( string databaseIndex, CancellationToken token = default ) where T : IDatabaseEntry;
	Task<bool> DeleteData<T>( IEnumerable<string> databaseIndexes, CancellationToken token = default ) where T : IDatabaseEntry;

	Task<T> GetData<T>( string dataId = "", CancellationToken token = default ) where T : IDatabaseEntry;

	Task<Dictionary<string, T>> GetBackup<T>( CancellationToken token = default ) where T : IDatabaseEntry;
	Task<List<string>> GetAllIndexes<T>( CancellationToken token = default ) where T : IDatabaseEntry;

	/// <summary>
	/// This overload calls back to <see cref="Add{T}(string, CancellationToken)"/>
	/// <para/> NOTE: this will not save the data itself, call db.SaveData for that
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="data"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	Task Add<T>( T data, CancellationToken token = default ) where T : IDatabaseEntry;

	/// <summary>
	/// This overload calls back to <see cref="Add{T}(IEnumerable{string}, CancellationToken)"/>
	/// <para/> NOTE: this will not save the data itself, call db.SaveData for that
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="databaseIndex"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	Task Add<T>( string databaseIndex, CancellationToken token = default ) where T : IDatabaseEntry;

	/// <summary>
	/// Adds these indexes to the EntryDataList of this type
	/// <para/>NOTE: this will not save the data itself, call db.SaveData for that
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="databaseIndexes"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	Task Add<T>( IEnumerable<string> databaseIndexes, CancellationToken token = default ) where T : IDatabaseEntry;

	Task Remove<T>( T data, CancellationToken token = default ) where T : IDatabaseEntry;
	Task Remove<T>( string databaseIndex, CancellationToken token = default ) where T : IDatabaseEntry;
	Task Remove<T>( IEnumerable<string> databaseIndexes, CancellationToken token = default ) where T : IDatabaseEntry;
}