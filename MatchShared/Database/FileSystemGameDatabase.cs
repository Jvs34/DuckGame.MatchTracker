using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class FileSystemGameDatabase : IGameDatabase
	{
		public SharedSettings SharedSettings { get; set; } = new SharedSettings();
		public bool ReadOnly => false;

		private JsonSerializer Serializer { get; } = new JsonSerializer()
		{
			Formatting = Formatting.Indented ,
			PreserveReferencesHandling = PreserveReferencesHandling.Objects ,
		};

		public async Task Load()
		{
			await Task.CompletedTask;
		}

		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			await Task.CompletedTask;

			string path = string.Empty;

			if( typeof( T ) == typeof( GlobalData ) )
			{
				path = SharedSettings.GetGlobalPath();
			}
			else if( typeof( T ) == typeof( MatchData ) )
			{
				path = SharedSettings.GetMatchPath( data.DatabaseIndex );
			}
			else if( typeof( T ) == typeof( RoundData ) )
			{
				path = SharedSettings.GetRoundPath( data.DatabaseIndex );
			}

			using( var stream = File.CreateText( path ) )
			{
				Serializer.Serialize( stream , data );
			}
		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			await Task.CompletedTask;

			string path = string.Empty;

			T data = default;

			if( typeof( T ) == typeof( GlobalData ) )
			{
				path = SharedSettings.GetGlobalPath();
			}
			else if( typeof( T ) == typeof( MatchData ) )
			{
				path = SharedSettings.GetMatchPath( dataId );
			}
			else if( typeof( T ) == typeof( RoundData ) )
			{
				path = SharedSettings.GetRoundPath( dataId );
			}

			if( File.Exists( path ) )
			{
				using( var stream = File.OpenText( path ) )
				using( JsonTextReader jsonReader = new JsonTextReader( stream ) )
				{
					data = Serializer.Deserialize<T>( jsonReader );
				}
			}

			return data;
		}
	}
}
