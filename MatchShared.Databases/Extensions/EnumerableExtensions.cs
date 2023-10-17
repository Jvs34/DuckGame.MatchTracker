using System.Collections.Generic;

namespace MatchShared.Databases.Extensions;

public static class EnumerableExtensions
{
	public static IEnumerable<T> AsSingleton<T>( T data )
	{
		yield return data;
	}
}
