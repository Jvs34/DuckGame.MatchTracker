using MatchShared.Databases;

namespace MatchViewer.Wasm;

public class BlazorGameDatabase : BaseGameDatabase
{
	public override async Task<T> GetData<T>( string dataId = "", CancellationToken token = default )
	{
		return default;
	}

	public override async Task Load( CancellationToken token = default )
	{
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
