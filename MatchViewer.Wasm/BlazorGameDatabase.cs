using MatchShared.Databases;

namespace MatchViewer.Wasm;

public class BlazorGameDatabase : BaseGameDatabase
{
	public override async Task<bool> DeleteData<T>( IEnumerable<string> databaseIndexes, CancellationToken token = default )
	{
		return false;
	}

	public override async Task<T> GetData<T>( string dataId = "", CancellationToken token = default )
	{
		return default;
	}

	public override async Task<bool> Load( CancellationToken token = default )
	{
		return false;
	}

	public override async Task<bool> SaveData<T>( T data, CancellationToken token = default )
	{
		return true;
	}

	protected override void DefineMappingInternal<T>()
	{

	}

	protected override void InternalDispose()
	{

	}
}
