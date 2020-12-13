using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class ZipGameDatabase : BaseGameDatabase
	{
		public override bool ReadOnly => Archive?.Mode == ZipArchiveMode.Read;
		public Stream ArchiveStream { get; set; }
		public ZipArchive Archive { get; protected set; }
		SemaphoreSlim StreamSemaphore { get; } = new SemaphoreSlim( 1 , 1 );
		private bool disposedValue;

		public override async Task Load()
		{
			if( ArchiveStream is null )
			{
				ArchiveStream = File.Open( SharedSettings.GetDatabasePath() , FileMode.Open );
			}

			Archive = new ZipArchive( ArchiveStream , ZipArchiveMode.Update , true );
			await Task.CompletedTask;
		}

		protected string ToZipPath( ZipArchive zipArchive , string path )
		{
			return path
				.Replace( SharedSettings.BaseRecordingFolder , zipArchive.Entries [0]?.FullName )
				.Replace( "\\" , "/" )
				.Replace( "//" , "/" );
		}

		public override async Task<T> GetData<T>( string dataId = "" )
		{
			await StreamSemaphore.WaitAsync();

			T data = default;
			var dataPath = ToZipPath( Archive , SharedSettings.GetDataPath<T>( dataId ) );

			var zipEntry = Archive.GetEntry( dataPath );
			if( zipEntry != null )
			{
				using var stream = zipEntry.Open();
				data = Deserialize<T>( stream );
			}

			StreamSemaphore.Release();

			return data;
		}

		public override async Task SaveData<T>( T data )
		{
			await StreamSemaphore.WaitAsync();

			var dataPath = ToZipPath( Archive , SharedSettings.GetDataPath<T>( data.DatabaseIndex ) );
			var zipEntry = Archive.GetEntry( dataPath ) ?? Archive.CreateEntry( dataPath );

			using( var entryStream = zipEntry.Open() )
			using( var writer = new StreamWriter( entryStream ) )
			{
				Serialize( data , writer );
				entryStream.SetLength( entryStream.Position );
			}

			zipEntry.LastWriteTime = DateTime.Now;
			StreamSemaphore.Release();
		}

		protected virtual void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					// TODO: dispose managed state (managed objects)
					Archive?.Dispose();
					ArchiveStream?.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		public override void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
	}
}
