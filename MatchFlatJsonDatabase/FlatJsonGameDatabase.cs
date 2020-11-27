using MatchTracker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MatchFlatJsonDatabase
{
	public sealed class FlatJsonGameDatabase : IGameDatabase, IDisposable
	{
		private bool disposedValue;
		public SharedSettings SharedSettings { get; set; }
		public bool ReadOnly => false;
		private Stream DatabaseStream { get; set; }
		private JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions( JsonSerializerDefaults.Web )
		{
			AllowTrailingCommas = true ,
			WriteIndented = true ,
		};
		private JsonDocumentOptions JsonDocumentOptions { get; } = new JsonDocumentOptions()
		{
			AllowTrailingCommas = true
		};

		SemaphoreSlim StreamSemaphore { get; } = new SemaphoreSlim( 1 , 1 );

		public async Task Load()
		{
			DatabaseStream = File.Open( SharedSettings.GetDatabasePath() , FileMode.OpenOrCreate , FileAccess.ReadWrite , FileShare.Read );
			await Task.CompletedTask;
		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			await StreamSemaphore.WaitAsync();
			T data = default;
			DatabaseStream.Position = 0;


			JsonDocument jsonDocument = await JsonDocument.ParseAsync( DatabaseStream , JsonDocumentOptions );
			

			//check if there's a root->TypeName property first, then root->TypeName->DatabaseIndex
			if( jsonDocument.RootElement.TryGetProperty( typeof( T ).Name , out var prop ) && prop.TryGetProperty( dataId , out var jsonData ) )
			{
				data = JsonSerializer.Deserialize<T>( jsonData.GetRawText() , JsonSerializerOptions );
			}

			StreamSemaphore.Release();
			return data;
		}

		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			await StreamSemaphore.WaitAsync();
			DatabaseStream.Position = 0;
			JsonDocument jsonDocument = await JsonDocument.ParseAsync( DatabaseStream , JsonDocumentOptions );
			var typeName = typeof( T ).Name;

			DatabaseStream.Position = 0;

			using( var writer = new Utf8JsonWriter( DatabaseStream , new JsonWriterOptions()
			{
				Indented = true
			} ) )
			{
				writer.WriteStartObject();

				var root = jsonDocument.RootElement;

				if( root.ValueKind == JsonValueKind.Object )
				{
					bool wroteData = false;

					foreach( var jsonTypeNameProp in root.EnumerateObject() )
					{
						//see if we can find TypeName->DatabaseIndex
						if( jsonTypeNameProp.NameEquals( typeName ) )
						{
							writer.WritePropertyName( typeName );
							writer.WriteStartObject();

							var typeProp = jsonTypeNameProp.Value;

							foreach( var dataProp in typeProp.EnumerateObject() )
							{
								//is this the property we need to override? otherwise keep writing
								if( dataProp.NameEquals( data.DatabaseIndex ) )
								{
									UtilWriteEntity( writer , data );
									wroteData = true;
								}
								else
								{
									dataProp.WriteTo( writer );
								}
							}

							if( !wroteData )
							{
								//append at the end
								UtilWriteEntity( writer , data );
								wroteData = true;
							}

							writer.WriteEndObject();
						}
						else
						{
							jsonTypeNameProp.WriteTo( writer );
						}

					}

					//can't even find the type written in the file, start it now
					if( !wroteData )
					{
						//TODO
						writer.WritePropertyName( typeName );
						writer.WriteStartObject();
						UtilWriteEntity( writer , data );
						writer.WriteEndObject();
					}

				}

				writer.WriteEndObject();

				DatabaseStream.SetLength( writer.BytesPending );
				await writer.FlushAsync();
			}
			StreamSemaphore.Release();
		}

		public async Task ImportBackup( Dictionary<string , Dictionary<string , IDatabaseEntry>> backup )
		{
			await StreamSemaphore.WaitAsync();
			DatabaseStream.Position = 0;

			using( var writer = new Utf8JsonWriter( DatabaseStream , new JsonWriterOptions()
			{
				Indented = true
			} ) )
			{
				writer.WriteStartObject();

				foreach( var typeKV in backup )
				{
					writer.WritePropertyName( typeKV.Key );
					writer.WriteStartObject();

					foreach( var dataKV in typeKV.Value )
					{
						UtilWriteEntityForNotGeneric( writer , dataKV.Value );
					}

					writer.WriteEndObject();
				}

				writer.WriteEndObject();

				DatabaseStream.SetLength( writer.BytesPending );
				await writer.FlushAsync();
			}
			StreamSemaphore.Release();
		}

		private void UtilWriteEntity<T>( Utf8JsonWriter writer , T data ) where T : IDatabaseEntry
		{
			writer.WritePropertyName( data.DatabaseIndex );
			//this is absolute garbage
			using( var dataDocument = JsonDocument.Parse( JsonSerializer.Serialize( data , JsonSerializerOptions ) ) )
			{
				dataDocument.RootElement.WriteTo( writer );
			}
		}

		private void UtilWriteEntityForNotGeneric( Utf8JsonWriter writer , IDatabaseEntry data )
		{
			writer.WritePropertyName( data.DatabaseIndex );
			//this is absolute garbage
			using( var dataDocument = JsonDocument.Parse( JsonSerializer.Serialize( data , data.GetType() , JsonSerializerOptions ) ) )
			{
				dataDocument.RootElement.WriteTo( writer );
			}
		}

		private void Dispose( bool disposing )
		{
			if( !disposedValue )
			{
				if( disposing )
				{
					// TODO: dispose managed state (managed objects)
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
