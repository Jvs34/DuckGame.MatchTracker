using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class ZipGameDatabase : BaseGameDatabase
	{
		public override bool ReadOnly => Archive?.Mode == ZipArchiveMode.Read;
		public ZipArchive Archive { get; protected set; }
		private bool disposedValue;

		public override async Task Load()
		{
			Archive = new ZipArchive( File.Open( SharedSettings.GetDatabasePath() , FileMode.Open ) , ZipArchiveMode.Update , false );
			await base.Load();
		}

		protected string ToZipPath( ZipArchive zipArchive , string path )
		{
			return path
				.Replace( SharedSettings.BaseRecordingFolder , zipArchive.Entries [0]?.FullName )
				.Replace( "\\" , "/" )
				.Replace( "//" , "/" );
		}

		public override Task<T> GetData<T>( string dataId = "" )
		{
			T data = default;
			var dataPath = ToZipPath( Archive , SharedSettings.GetDataPath<T>( dataId ) );

			var zipEntry = Archive.GetEntry( dataPath );
			if( zipEntry != null )
			{
				using var stream = zipEntry.Open();
				data = Deserialize<T>( stream );
			}

			return Task.FromResult( data );
		}

		public override Task SaveData<T>( T data )
		{
			var dataPath = ToZipPath( Archive , SharedSettings.GetDataPath<T>( data.DatabaseIndex ) );
			var zipEntry = Archive.GetEntry( dataPath ) ?? Archive.CreateEntry( dataPath );

			using( var entryStream = zipEntry.Open() )
			using( var writer = new StreamWriter( entryStream ) )
			{
				Serialize( data , writer );
			}

			zipEntry.LastWriteTime = DateTime.Now;
			return Task.CompletedTask;
		}

		protected virtual void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					// TODO: dispose managed state (managed objects)
					Archive?.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~ZipGameDatabase()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public override void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
	}
}
