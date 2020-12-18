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

		public JsonSerializer Serializer { get; } = new JsonSerializer()
		{
			Formatting = Formatting.Indented
		};

		public virtual bool InitialLoad { get; set; }

		public virtual async Task Load()
		{
			if( InitialLoad )
			{
				await LoadEverything();
			}
		}

		/// <summary>
		/// Loads everything from the EntryData tree
		/// </summary>
		/// <returns></returns>
		protected virtual Task LoadEverything()
		{
			return Task.CompletedTask;
		}

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

		#region INTERFACE
		public abstract Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry;
		public abstract Task SaveData<T>( T data ) where T : IDatabaseEntry;
		public abstract void Dispose();
		#endregion
	}
}
