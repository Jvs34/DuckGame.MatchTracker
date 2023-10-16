using DuckGame;
using MatchRecorder.Hooks;
using MatchRecorder.Shared.Enums;
using MatchRecorder.Shared.Interfaces;
using MatchRecorder.Shared.Messages;
using MatchRecorder.Shared.Settings;
using MatchRecorder.Utils;
using MatchShared.DataClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder;

public sealed class MatchRecorderClient : IDisposable
{
	public const string DuckGameAuthor = "superjoebob";

	private bool IsDisposed { get; set; }
	private CancellationTokenSource StopTokenSource { get; }
	private CancellationToken StopToken { get; } = CancellationToken.None;
	public string ModPath { get; }
	private Process RecorderProcess { get; set; }
	private MessageHandlers.HttpMessageHandler MessageHandler { get; }
	private Task MessageHandlerTask { get; set; }
	public string RecorderUrl { get; set; } = "http://localhost:6969";
	private HttpClient HttpClient { get; }
	private ModSettings Settings { get; set; } = new ModSettings()
	{
		RecorderType = RecorderType.OBSRawVideo,
		RecordingEnabled = true,
	};
	public MatchRecorderMenu SettingsMenu { get; set; }
	private JsonSerializer Serializer { get; } = new JsonSerializer()
	{
		Formatting = Formatting.Indented
	};
	private string SettingsPath { get; }

	public MatchRecorderClient( string directory )
	{
		ModPath = directory;
		SettingsPath = Path.Combine( ModPath, "modsettings.json" );
		StopTokenSource = new CancellationTokenSource();
		StopToken = CancellationToken.None;
		HttpClient = new HttpClient()
		{
			BaseAddress = new Uri( RecorderUrl ),
			Timeout = TimeSpan.FromSeconds( 10 )
		};
		HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd( "duckgame-matchrecorder/1.0" );

		MessageHandler = new MessageHandlers.HttpMessageHandler( HttpClient, StopToken );
		MessageHandler.OnReceiveMessage += OnReceiveMessage;

		LoadSettings();
		SaveSettings();

		SettingsMenu = new MatchRecorderMenu();
		SettingsMenu.SetOptions += SetMenuOptions;
		SettingsMenu.GetOptions += GetMenuOptions;
		SettingsMenu.ApplyOptions += ApplyMenuOptions;
		SettingsMenu.RestartCompanion += RestartCompanion;
		SettingsMenu.GenerateThumbnails += GenerateThumbnails;
	}

	private void GenerateThumbnails()
	{


		var levels = Content.GetAllLevels();

		var rt = new RenderTarget2D( 1920, 1080 );

		foreach( var level in levels )
		{
			if( level.metaData.type != LevelType.Deathmatch )
			{
				continue;
			}

			Content.GeneratePreview( level, true, rt );

			var textArray = new Color[1920 * 1080];

			rt.GetData( textArray );

			var test = 5;
		}

		CollectLevelData();
	}

	private void RestartCompanion()
	{
		MessageHandler?.SendMessage( new CloseRecorderMessage() );
	}

	private void ApplyMenuOptions()
	{

	}

	private void SetMenuOptions( ModSettings menuOptions )
	{
		Settings.RecordingEnabled = menuOptions.RecordingEnabled;
		Settings.RecorderType = menuOptions.RecorderType;
		SaveSettings();
	}

	private ModSettings GetMenuOptions()
	{
		return Settings;
	}

	public bool LoadSettings()
	{
		if( !File.Exists( SettingsPath ) )
		{
			return false;
		}

		using var dataStream = File.Open( SettingsPath, FileMode.Open );
		using var reader = new StreamReader( dataStream );
		using var jsonReader = new JsonTextReader( reader );
		Settings = Serializer.Deserialize<ModSettings>( jsonReader );
		return true;
	}

	public void SaveSettings()
	{
		using var writer = File.CreateText( SettingsPath );
		Serializer.Serialize( writer, Settings );
	}

	private void OnReceiveMessage( BaseMessage message )
	{
		if( message is TextMessage txtMessage )
		{
			ShowHUDMessage( txtMessage.Message, position: txtMessage.MessagePosition );
		}
	}

	public static void ShowHUDMessage( string text, float lifetime = 1.5f, TextMessagePosition position = TextMessagePosition.TopLeft )
	{
		var cornerMessage = HUD.AddCornerMessage( (HUDCorner) position, text, true );
		cornerMessage.willDie = true;
		cornerMessage.life = lifetime;
	}

	internal void Update()
	{
		if( ( Keyboard.Pressed( Keys.F7 ) || Input.Pressed( "GRAB" ) ) && Level.current is TitleScreen tScreen )
		{
			SettingsMenu.ShowUI( tScreen );
		}

		if( StopToken.IsCancellationRequested )
		{
			return;
		}

		CheckRecorderProcess();
		MessageHandler?.UpdateMessages();

		if( ( MessageHandlerTask is null || MessageHandlerTask.IsCompleted == true ) && MessageHandler != null )
		{
			StartMessageHandlerTask();
		}
	}

	private void StartMessageHandlerTask()
	{
		System.Diagnostics.Debug.WriteLine( "Starting MessageHandler task" );
		//TODO: switch to a thread? tasks are not guaranteed to be ran in their own thread
		MessageHandlerTask = Task.Run( async () =>
		{
			try
			{
				await MessageHandler.ThreadedLoop( StopToken );
			}
			catch( Exception e )
			{
				System.Diagnostics.Debug.WriteLine( e );
			}
		}, StopToken );
	}

	private void CheckRecorderProcess()
	{
		if( !IsRecorderProcessAlive() )
		{
			StartRecorderProcess();
		}
	}

	private bool IsRecorderProcessAlive() => RecorderProcess != null && !RecorderProcess.HasExited;

	private void StartRecorderProcess()
	{
		var startInfo = new ProcessStartInfo()
		{
			FileName = Path.Combine( ModPath, "MatchRecorder.OOP", "MatchRecorder.OOP.exe" ),
			WorkingDirectory = ModPath,
			UseShellExecute = false,
			CreateNoWindow = false,
			WindowStyle = ProcessWindowStyle.Minimized,
		};

		var envVars = startInfo.EnvironmentVariables;
		envVars.Add( "ASPNETCORE_URLS", $"{RecorderUrl}" );
		envVars.Add( nameof( IRecorderSharedSettings.RecorderType ), $"{Settings.RecorderType}" );
		envVars.Add( nameof( IRecorderSharedSettings.RecordingEnabled ), $"{Settings.RecordingEnabled}" );
		envVars.Add( nameof( RecorderSettings.DuckGameProcessID ), $"{Process.GetCurrentProcess().Id}" );
		envVars.Add( nameof( RecorderSettings.AutoCloseWhenParentDies ), $"{true}" );

		RecorderProcess = Process.Start( startInfo );
	}

	#region MATCHTRACKING
	internal void StartRecordingMatch() => MessageHandler?.SendMessage( new StartMatchMessage()
	{
		Teams = Teams.active.Select( RecorderUtils.ConvertDuckGameTeamToTeamData ).ToList(),
		Players = Profiles.activeNonSpectators.Select( RecorderUtils.GetPlayerID ).ToList(),
		PlayersData = Profiles.active.Select( RecorderUtils.ConvertDuckGameProfileToPlayerData ).ToList(),
		TimeStarted = DateTime.Now,
	} );

	internal void StopRecordingMatch( bool aborted = false ) => MessageHandler?.SendMessage( new EndMatchMessage()
	{
		Aborted = aborted,
		Teams = Teams.active.Select( RecorderUtils.ConvertDuckGameTeamToTeamData ).ToList(),
		Players = Profiles.activeNonSpectators.Select( RecorderUtils.GetPlayerID ).ToList(),
		PlayersData = Profiles.active.Select( RecorderUtils.ConvertDuckGameProfileToPlayerData ).ToList(),
		Winner = RecorderUtils.ConvertDuckGameTeamToTeamData( Teams.winning.FirstOrDefault() ),
		TimeEnded = DateTime.Now,
	} );

	internal void StartRecordingRound() => MessageHandler?.SendMessage( new StartRoundMessage()
	{
		LevelName = Level.current.level,
		Teams = Teams.active.Select( RecorderUtils.ConvertDuckGameTeamToTeamData ).ToList(),
		Players = Profiles.activeNonSpectators.Select( RecorderUtils.GetPlayerID ).ToList(),
		TimeStarted = DateTime.Now,
	} );

	internal void StopRecordingRound() => MessageHandler?.SendMessage( new EndRoundMessage()
	{
		Teams = Teams.active.Select( RecorderUtils.ConvertDuckGameTeamToTeamData ).ToList(),
		Players = Profiles.activeNonSpectators.Select( RecorderUtils.GetPlayerID ).ToList(),
		Winner = RecorderUtils.ConvertDuckGameTeamToTeamData( GameMode.lastWinners.FirstOrDefault()?.team ),
		TimeEnded = DateTime.Now,
	} );

	internal void TrackKill( Duck duckVictim, DestroyType type, bool isNetworkMessage )
	{
		Profile killerProfile = null;

		var objectResponsible = string.Empty;

		if( type != null )
		{
			var kv = RecorderUtils.GetBestDestroyTypeKillerAndWeapon( type );
			killerProfile = kv.Key;
			objectResponsible = kv.Value;
		}

		if( isNetworkMessage )
		{
			//in unmodded duck game, we're very restricted by what we can get on the network

			killerProfile = NMKillDuck_Activate.CurrentNMKillDuckConnection?.profile;

			//TODO: check if the companion mod is installed and then try get the additional data

		}

		TeamData killerTeamData = null;

		if( killerProfile != null )
		{
			killerTeamData = RecorderUtils.ConvertDuckGameProfileToTeamData( killerProfile );
		}

		var killData = new KillData()
		{
			Killer = killerTeamData,
			Victim = RecorderUtils.ConvertDuckGameProfileToTeamData( duckVictim.profile ),
			DeathTypeClassName = type?.GetType()?.Name,
			TimeOccured = DateTime.Now,
			ObjectClassName = objectResponsible
		};

		MessageHandler?.SendMessage( new TrackKillMessage()
		{
			KillData = killData
		} );

		CollectObjectData( type.thing );
	}

	internal void CollectObjectData( Thing thing )
	{
		if( thing == null )
		{
			return;
		}

		var collectObjectDataMessage = new CollectObjectDataMessage()
		{
			ObjectDataList = new List<ObjectData>()
		};

		collectObjectDataMessage.ObjectDataList.Add( RecorderUtils.ConvertThingToObjectData( thing ) );

		var bulletOwnerData = RecorderUtils.ConvertThingToObjectData( thing is Bullet boolet ? boolet.firedFrom : thing.responsibleProfile?.duck );

		if( bulletOwnerData != null )
		{
			collectObjectDataMessage.ObjectDataList.Add( bulletOwnerData );
		}

		var ownerData = RecorderUtils.ConvertThingToObjectData( thing.owner ?? thing.prevOwner );

		if( ownerData != null )
		{
			collectObjectDataMessage.ObjectDataList.Add( ownerData );
		}

		MessageHandler?.SendMessage( collectObjectDataMessage );
	}

	internal void CollectLevelData( DuckGame.LevelData duckGameLevel = null, bool onlyTrackSingleData = false )
	{
		if( duckGameLevel is null && onlyTrackSingleData )
		{
			return;
		}

		List<DuckGame.LevelData> levels = duckGameLevel is null ? Content.GetAllLevels() : new List<DuckGame.LevelData>() { duckGameLevel };

		var mtLevels = new List<MatchShared.DataClasses.LevelData>();

		foreach( var level in levels )
		{
			if( level.metaData is null || level.metaData.type != LevelType.Deathmatch )
			{
				continue;
			}

			var author = level.workshopData?.author ?? string.Empty;

			//force the author to be duckgame's creator, as he didn't seem to be diligent in filling in the metadata
			if( level.GetLocation() == LevelLocation.Content )
			{
				author = DuckGameAuthor;
			}

			mtLevels.Add( new()
			{
				LevelName = level.metaData.guid,
				Author = author,
				FilePath = level.GetPath(),
				Description = level.workshopData?.description,
				IsOnlineMap = level.metaData.online,
				IsCustomMap = level.GetLocation() != LevelLocation.Content,
				IsEightPlayerMap = level.metaData.eightPlayer,
				IsOnlyEightPlayer = level.metaData.eightPlayerRestricted
			} );
		}

		MessageHandler?.SendMessage( new CollectLevelDataMessage()
		{
			Levels = mtLevels
		} );
	}

	#endregion MATCHTRACKING

	private void Dispose( bool disposing )
	{
		if( !IsDisposed )
		{
			if( disposing )
			{
				StopTokenSource.Cancel();
				MessageHandlerTask = null;
			}

			IsDisposed = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose( disposing: true );
		GC.SuppressFinalize( this );
	}
}
