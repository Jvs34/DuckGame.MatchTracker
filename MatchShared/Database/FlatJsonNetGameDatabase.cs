using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchTracker
{
	public sealed class FlatJsonNetGameDatabase : IGameDatabase, IDisposable
	{
		private bool disposedValue;
		public SharedSettings SharedSettings { get; set; } = new SharedSettings();
		public bool ReadOnly => false;
		private Stream DatabaseStream { get; set; }
		SemaphoreSlim StreamSemaphore { get; } = new SemaphoreSlim( 1 , 1 );
		public JsonSerializer Serializer { get; } = new JsonSerializer()
		{
			Formatting = Formatting.Indented
		};

		public async Task Load()
		{
			DatabaseStream = File.Open( SharedSettings.GetDatabasePath() , FileMode.OpenOrCreate , FileAccess.ReadWrite , FileShare.ReadWrite );
			await Task.CompletedTask;
		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			if( string.IsNullOrEmpty( dataId ) )
			{
				dataId = typeof( T ).Name;
			}

			await StreamSemaphore.WaitAsync();
			T data = default;
			DatabaseStream.Position = 0;

			var jsonPath = $"{typeof( T ).Name}['{dataId}']";
			using( var streamReader = new StreamReader( DatabaseStream , Encoding.UTF8 , true , 1024 , true ) )
			using( JsonReader reader = new JsonTextReader( streamReader )
			{
				CloseInput = false ,
			} )
			{
				while( await reader.ReadAsync() )
				{
					if( reader.TokenType == JsonToken.StartObject && reader.Path.Equals( jsonPath , StringComparison.InvariantCultureIgnoreCase ) )
					{
						data = Serializer.Deserialize<T>( reader );
						break;
					}

					if( data != null )
					{
						break;
					}
				}
			}

			StreamSemaphore.Release();

			return data;
		}

		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			await StreamSemaphore.WaitAsync();
			DatabaseStream.Position = 0;

			var tempPath = Path.Combine( Path.GetTempPath() , "MatchRecorder" );
			var tempFilePath = Path.Combine( tempPath , SharedSettings.DatabaseFile );
			if( !Directory.Exists( tempPath ) )
			{
				Directory.CreateDirectory( tempPath );
			}

			await CopyDatabaseTo( tempFilePath );

			DatabaseStream.Position = 0;

			var typeName = typeof( T ).Name;
			var jsonPath = $"{typeof( T ).Name}['{data.DatabaseIndex}']";

			//jesus christ what a pain in the ass, seems like every bloody thing here closes the stream
			//so this looks needlessly UGLY
			using( var fileCopyStream = File.OpenRead( tempFilePath ) )
			using( var streamReader = new StreamReader( fileCopyStream ) )
			using( var reader = new JsonTextReader( streamReader ) )
			using( var streamWriter = new StreamWriter( DatabaseStream , Encoding.UTF8 , 1024 , true ) )
			using( var writer = new JsonTextWriter( streamWriter )
			{
				Formatting = Formatting.Indented ,
				CloseOutput = false
			} )
			{
				bool wroteData = false;

				await writer.WriteStartObjectAsync();

				//go through all the top properties, MatchData etc





				if( !wroteData )
				{

				}

				await writer.WriteEndObjectAsync();
				DatabaseStream.SetLength( DatabaseStream.Position );
				await writer.FlushAsync();
			}

			StreamSemaphore.Release();
		}

		private async Task CopyJsonProperty( JsonReader reader , JsonWriter writer )
		{
			await Task.CompletedTask;
		}

		private async Task CopyDatabaseTo( string pathto )
		{
			DatabaseStream.Position = 0;
			using( var tostream = File.OpenWrite( pathto ) )
			{
				await DatabaseStream.CopyToAsync( tostream );
				tostream.SetLength( DatabaseStream.Length );
			}
		}

		private void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					DatabaseStream?.Dispose();
					DatabaseStream = null;
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
	}
}
