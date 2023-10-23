using MatchShared.Databases.Extensions;
using MatchShared.Databases.Interfaces;
using MatchShared.Databases.Settings;
using MatchShared.DataClasses;
using MatchShared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatchShared.Databases;

public abstract class BaseGameDatabase : IGameDatabase, IDisposable
{
	protected bool IsDisposed { get; private set; }
	public SharedSettings SharedSettings { get; set; } = new SharedSettings();
	public virtual bool IsReadOnly => false;
	public virtual bool IsLoaded => IsLoadedInternal;
	protected bool IsLoadedInternal { get; set; }

	protected BaseGameDatabase()
	{
		DefineMapping<EntryListData>();
		DefineMapping<RoundData>();
		DefineMapping<MatchData>();
		DefineMapping<TournamentData>();
		DefineMapping<LevelData>();
		DefineMapping<TagData>();
		DefineMapping<PlayerData>();
		DefineMapping<ObjectData>();
		DefineMapping<DestroyTypeData>();
	}

	private void DefineMapping<T>() where T : IDatabaseEntry
	{
		//TODO: save a backup callback for each type defined
		DefineMappingInternal<T>();
	}

	public abstract Task<bool> Load( CancellationToken token = default );
	protected abstract void DefineMappingInternal<T>() where T : IDatabaseEntry;
	protected abstract void InternalDispose();

	public abstract Task<T> GetData<T>( string dataId = "", CancellationToken token = default ) where T : IDatabaseEntry;
	public abstract Task<bool> SaveData<T>( T data, CancellationToken token = default ) where T : IDatabaseEntry;
	public virtual async Task<bool> SaveData<T>( IEnumerable<T> datas, CancellationToken token = default ) where T : IDatabaseEntry
	{
		bool allsuccess = true;

		foreach( var data in datas )
		{
			if( !await SaveData( data, token ) )
			{
				allsuccess = false;
			}
		}

		return allsuccess;
	}

	public virtual async Task<bool> DeleteData<T>( T data, CancellationToken token = default ) where T : IDatabaseEntry => await DeleteData( EnumerableExtensions.AsSingleton( data ), token );
	public virtual async Task<bool> DeleteData<T>( IEnumerable<T> datas, CancellationToken token = default ) where T : IDatabaseEntry => await DeleteData<T>( datas.Select( x => x.DatabaseIndex ), token );
	public virtual async Task<bool> DeleteData<T>( string databaseIndex, CancellationToken token = default ) where T : IDatabaseEntry => await DeleteData<T>( EnumerableExtensions.AsSingleton( databaseIndex ), token );
	public abstract Task<bool> DeleteData<T>( IEnumerable<string> databaseIndexes, CancellationToken token = default ) where T : IDatabaseEntry;

	public virtual async Task<Dictionary<string, T>> GetBackup<T>( CancellationToken token = default ) where T : IDatabaseEntry
	{
		var entryNames = await GetAllIndexes<T>( token );

		var dataTasks = entryNames.Select( entryName => GetData<T>( entryName, token ) ).ToList();

		var results = await Task.WhenAll( dataTasks );

		return results
			.Select( x => new KeyValuePair<string, T>( x.DatabaseIndex, x ) )
			.ToDictionary( x => x.Key, x => x.Value );
	}

	public virtual async Task<List<string>> GetAllIndexes<T>( CancellationToken token = default ) where T : IDatabaseEntry
	{
		var entryListData = await GetData<EntryListData>( typeof( T ).Name, token );

		return entryListData?.Entries.ToList() ?? new();
	}

	public virtual async Task Add<T>( T data, CancellationToken token = default ) where T : IDatabaseEntry => await Add<T>( data.DatabaseIndex, token );
	public virtual async Task Add<T>( string databaseIndex, CancellationToken token = default ) where T : IDatabaseEntry => await Add<T>( EnumerableExtensions.AsSingleton( databaseIndex ), token );
	public virtual async Task Add<T>( IEnumerable<string> databaseIndexes, CancellationToken token = default ) where T : IDatabaseEntry
	{
		var typeName = typeof( T ).Name;

		var entryListData = await GetData<EntryListData>( typeName, token );

		bool doAdd = false;

		if( entryListData == null )
		{
			entryListData = new EntryListData()
			{
				Type = typeName
			};

			//signal that we need to add this to the EntryListData itself
			doAdd = entryListData.Type != typeof( EntryListData ).Name;
		}

		entryListData.Entries.UnionWith( databaseIndexes );

		if( doAdd )
		{
			await Add( entryListData, token );
		}

		await SaveData( entryListData, token );
	}

	public virtual async Task Remove<T>( T data, CancellationToken token = default ) where T : IDatabaseEntry => await Remove<T>( data.DatabaseIndex, token );
	public virtual async Task Remove<T>( string databaseIndex, CancellationToken token = default ) where T : IDatabaseEntry => await Remove<T>( EnumerableExtensions.AsSingleton( databaseIndex ), token );
	public virtual async Task Remove<T>( IEnumerable<string> databaseIndexes, CancellationToken token = default ) where T : IDatabaseEntry
	{
		var typeName = typeof( T ).Name;
		var entryListData = await GetData<EntryListData>( typeName, token );

		if( entryListData is null )
		{
			return;
		}

		entryListData.Entries.ExceptWith( databaseIndexes );

		await SaveData( entryListData, token );
	}

	protected virtual void Dispose( bool disposing )
	{
		if( !IsDisposed )
		{
			if( disposing )
			{
				InternalDispose();
			}

			IsDisposed = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose( disposing: true );
		GC.SuppressFinalize( this );
	}
}
