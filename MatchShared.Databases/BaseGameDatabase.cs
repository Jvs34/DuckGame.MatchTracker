using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker;

public abstract class BaseGameDatabase : IGameDatabase, IDisposable
{
	protected bool IsDisposed { get; private set; }
	public SharedSettings SharedSettings { get; set; } = new SharedSettings();
	public virtual bool IsReadOnly => false;

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

	public abstract Task Load( CancellationToken token = default );
	protected abstract void DefineMappingInternal<T>() where T : IDatabaseEntry;
	public abstract Task<T> GetData<T>( string dataId = "" , CancellationToken token = default ) where T : IDatabaseEntry;
	public abstract Task<bool> SaveData<T>( T data , CancellationToken token = default ) where T : IDatabaseEntry;
	protected abstract void InternalDispose();

	public virtual async Task<Dictionary<string , T>> GetBackup<T>() where T : IDatabaseEntry
	{
		var entryNames = await GetAllIndexes<T>();

		var dataTasks = entryNames.Select( entryName => GetData<T>( entryName ) ).ToList();

		var results = await Task.WhenAll( dataTasks );

		return results
			.Select( x => new KeyValuePair<string , T>( x.DatabaseIndex , x ) )
			.ToDictionary( x => x.Key , x => x.Value );
	}

	public virtual async Task<List<string>> GetAllIndexes<T>() where T : IDatabaseEntry
	{
		var databaseIndexes = new List<string>();

		var entryListData = await GetData<EntryListData>( typeof( T ).Name );
		if( entryListData != null )
		{
			databaseIndexes.AddRange( entryListData.Entries );
		}

		return databaseIndexes;
	}

	public virtual async Task Add<T>( T data ) where T : IDatabaseEntry => await Add<T>( data.DatabaseIndex );

	public virtual async Task Add<T>( params string [] databaseIndexes ) where T : IDatabaseEntry
	{
		var entryListData = await GetData<EntryListData>( typeof( T ).Name );

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
			await Add( entryListData );
		}

		await SaveData( entryListData );
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
