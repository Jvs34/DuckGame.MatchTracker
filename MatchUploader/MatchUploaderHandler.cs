using MatchTracker;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MatchUploader;

/// <summary>
/// Goes through all the folders, puts all rounds and matches into data.json
/// Also returns match/round data from the timestamped name and whatnot
/// </summary>
public sealed class MatchUploaderHandler : IDisposable
{
	private bool disposedValue;
	private IGameDatabase GameDatabase { get; }
	private string SettingsFolder { get; }
	private UploaderSettings UploaderSettings { get; } = new UploaderSettings();
	private IConfigurationRoot Configuration { get; }
	private List<Uploader> Uploaders { get; } = new List<Uploader>();
	private JsonSerializer Serializer { get; } = new JsonSerializer()
	{
		Formatting = Formatting.Indented
	};

	public MatchUploaderHandler( string [] args )
	{
		SettingsFolder = Path.Combine( Directory.GetCurrentDirectory() , "Settings" );
		Configuration = new ConfigurationBuilder()
			.SetBasePath( SettingsFolder )
			.AddJsonFile( "shared.json" )
			.AddJsonFile( "uploader.json" )
			.AddJsonFile( "bot.json" )
			.AddCommandLine( args )
		.Build();

		Configuration.Bind( UploaderSettings );

		GameDatabase = new LiteDBGameDatabase();
		Configuration.Bind( GameDatabase.SharedSettings );

		CreateUploaders();
	}

	private void CreateUploaders()
	{
		//AddUploader( new MatchMerger( GameDatabase , UploaderSettings ) );
		AddUploader( new YoutubeMatchUpdater( GameDatabase , UploaderSettings ) );
		//AddUploader( new YoutubeRoundUploader( GameDatabase , UploaderSettings ) );
	}

	private void AddUploader( Uploader uploader )
	{
		var typeName = uploader.GetType().Name;

		if( !UploaderSettings.UploadersInfo.TryGetValue( typeName , out var uploaderInfo ) )
		{
			UploaderSettings.UploadersInfo.TryAdd( typeName , uploader.Info );
			uploaderInfo = uploader.Info;
		}

		uploader.Info = uploaderInfo;

		Uploaders.Add( uploader );
	}

	public async Task Initialize()
	{
		foreach( var uploader in Uploaders )
		{
			uploader.SaveSettingsCallback += SaveSettings;

			if( !uploader.Info.HasBeenSetup )
			{
				uploader.SetupDefaultInfo();
				uploader.Info.HasBeenSetup = true;
			}

			await uploader.Initialize();
		}
	}

	public async Task LoadDatabase()
	{
		Console.WriteLine( $"Loading the {GameDatabase.GetType()}" );
		await GameDatabase.Load();
		Console.WriteLine( $"Finished loading the {GameDatabase.GetType()}" );
	}

	public async Task RunAsync()
	{
		await LoadDatabase();
		await Initialize();
		SaveSettings();
		await Upload();
		SaveSettings();
	}

	private async Task Upload()
	{
		foreach( var uploader in Uploaders )
		{
			if( uploader.Info.Enabled )
			{
				await uploader.UploadAll();
			}
		}
	}

	public void SaveSettings()
	{
		using var writer = File.CreateText( Path.Combine( SettingsFolder , "uploader.json" ) );
		Serializer.Serialize( writer , UploaderSettings );
	}

	private void Dispose( bool disposing )
	{
		if( !disposedValue )
		{
			if( disposing )
			{
				GameDatabase.Dispose();

			}

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