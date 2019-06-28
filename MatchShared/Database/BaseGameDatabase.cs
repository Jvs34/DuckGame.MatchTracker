using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MatchTracker
{
	public abstract class BaseGameDatabase : IGameDatabase
	{
		public SharedSettings SharedSettings { get; set; } = new SharedSettings();
		public virtual bool ReadOnly => false;
		protected Dictionary<string , Dictionary<string , IDatabaseEntry>> Cache { get; } = new Dictionary<string , Dictionary<string , IDatabaseEntry>>();
		protected Dictionary<string , Dictionary<string , DateTime>> CacheExpireTime { get; } = new Dictionary<string , Dictionary<string , DateTime>>();

		public JsonSerializer Serializer { get; } = new JsonSerializer()
		{
			Formatting = Formatting.Indented
		};

		#region JSON
		protected virtual void Serialize<T>( T data , TextWriter textWriter ) where T : IDatabaseEntry
		{
			using( JsonTextWriter jsonTextWriter = new JsonTextWriter( textWriter ) )
			{
				Serializer.Serialize( textWriter , data );
			}
		}

		protected virtual T Deserialize<T>( Stream dataStream ) where T : IDatabaseEntry
		{
			T data = default;
			using( StreamReader reader = new StreamReader( dataStream ) )
			using( JsonTextReader jsonReader = new JsonTextReader( reader ) )
			{
				data = Serializer.Deserialize<T>( jsonReader );
			}

			return data;
		}
		#endregion

		#region CACHE
		protected void ClearCache()
		{
			lock( Cache )
			{
				lock( CacheExpireTime )
				{
					Cache.Clear();
					CacheExpireTime.Clear();
				}
			}
		}

		protected void ClearExpiredCache()
		{
			//TODO
		}

		protected T GetCachedItem<T>( string databaseIndex )
		{
			lock( Cache )
			{
				if( Cache.TryGetValue( typeof( T ).Name , out var keyValues ) && keyValues.TryGetValue( databaseIndex , out var data ) )
				{
					return (T) data;
				}
			}
			return default;
		}

		protected void SetCachedItem<T>( T data , DateTime expire ) where T : IDatabaseEntry
		{
			string typeName = typeof( T ).Name;

			lock( Cache )
			{
				lock( CacheExpireTime )
				{
					if( !Cache.ContainsKey( typeName ) )
					{
						Cache [typeName] = new Dictionary<string , IDatabaseEntry>();
					}

					if( !CacheExpireTime.ContainsKey( typeName ) )
					{
						CacheExpireTime [typeName] = new Dictionary<string , DateTime>();
					}

					Cache [typeName] [data.DatabaseIndex] = data;
					CacheExpireTime [typeName] [data.DatabaseIndex] = expire;
				}
			}
		}

		protected bool IsCachedItemExpired<T>( string databaseIndex , DateTime checkedAgainst ) where T : IDatabaseEntry
		{
			lock( CacheExpireTime )
			{
				if( CacheExpireTime.TryGetValue( typeof( T ).Name , out var keyValues ) && keyValues.TryGetValue( databaseIndex , out var date ) )
				{
					return date < checkedAgainst;
				}
			}
			return true;
		}
		#endregion

		#region INTERFACE
		public abstract Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry;

		public abstract Task Load();

		public abstract Task SaveData<T>( T data ) where T : IDatabaseEntry;
		#endregion
	}
}
