﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker;


public interface IGameDatabase : IDisposable
{
	SharedSettings SharedSettings { get; set; }

	bool IsReadOnly { get; }

	Task Load( CancellationToken token = default );
	Task<bool> SaveData<T>( T data , CancellationToken token = default ) where T : IDatabaseEntry;
	Task<T> GetData<T>( string dataId = "" , CancellationToken token = default ) where T : IDatabaseEntry;
	Task<Dictionary<string , T>> GetBackup<T>() where T : IDatabaseEntry;
	Task<List<string>> GetAllIndexes<T>() where T : IDatabaseEntry;
	
	/// <summary>
	/// <para>Adds this item to EntryListData of this type</para>
	/// <para>
	/// This overload calls back to IGameDatabase.Add( string databaseIndex )
	/// NOTE: this will not save the data itself, call db.SaveData for that
	/// </para>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="data"></param>
	Task Add<T>( T data ) where T : IDatabaseEntry;
	Task Add<T>( params string [] databaseIndexes ) where T : IDatabaseEntry;
}