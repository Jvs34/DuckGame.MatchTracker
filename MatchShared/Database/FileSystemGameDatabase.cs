using System;
using System.IO;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class FileSystemGameDatabase : BaseGameDatabase
	{
		public override bool ReadOnly => false;

		public override async Task Load()
		{
			await Task.CompletedTask;
		}

		public override async Task SaveData<T>( T data )
		{
			await Task.CompletedTask;

			try
			{
				Directory.CreateDirectory( SharedSettings.GetPath<T>( data.DatabaseIndex ) );
			}
			catch( Exception e )
			{
				Console.WriteLine( e );
			}

			string path = SharedSettings.GetDataPath<T>( data.DatabaseIndex );

			using( var stream = File.CreateText( path ) )
			{
				Serialize( data , stream );
			}
		}

		public override async Task<T> GetData<T>( string dataId = "" )
		{
			await Task.CompletedTask;

			string path = SharedSettings.GetDataPath<T>( dataId );

			T data = default;

			if( File.Exists( path ) )
			{
				using( var stream = File.Open( path , FileMode.Open ) )
				{
					data = Deserialize<T>( stream );
				}
			}

			return data;
		}
	}
}
