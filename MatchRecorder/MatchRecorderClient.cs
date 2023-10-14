using DuckGame;
using MatchRecorder.Hooks;
using MatchRecorder.Shared.Enums;
using MatchRecorder.Shared.Interfaces;
using MatchRecorder.Shared.Messages;
using MatchRecorder.Shared.Settings;
using MatchShared.DataClasses;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
		foreach( var level in levels )
		{
			if( level.metaData.type != LevelType.Deathmatch )
			{
				continue;
			}

			//TODO: use reflection to call content.preview.GetData and then content.preview.Dispose to free data
			var content = Content.GeneratePreview( level ); //new RenderTarget2D( 1920, 1080 )
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
			ShowHUDMessage( txtMessage.Message );
		}
	}

	public static void ShowHUDMessage( string text, float lifetime = 1f, TextMessagePosition position = TextMessagePosition.TopLeft )
	{
		var cornerMessage = HUD.AddCornerMessage( (HUDCorner) position, text, true );
		cornerMessage.slide = 1;
		cornerMessage.willDie = true;
		cornerMessage.life = lifetime;
	}

	internal void Update()
	{
		if( StopToken.IsCancellationRequested )
		{
			return;
		}

		if( ( Keyboard.Pressed( Keys.F7 ) || Input.Pressed( "GRAB" ) ) && Level.current is TitleScreen tScreen )
		{
			SettingsMenu.ShowUI( tScreen );
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
		Teams = Teams.active.Select( ConvertDuckGameTeamToTeamData ).ToList(),
		Players = Profiles.activeNonSpectators.Select( GetPlayerID ).ToList(),
		PlayersData = Profiles.active.Select( ConvertDuckGameProfileToPlayerData ).ToList(),
		TimeStarted = DateTime.Now,
	} );

	internal void StopRecordingMatch( bool aborted = false ) => MessageHandler?.SendMessage( new EndMatchMessage()
	{
		Aborted = aborted,
		Teams = Teams.active.Select( ConvertDuckGameTeamToTeamData ).ToList(),
		Players = Profiles.activeNonSpectators.Select( GetPlayerID ).ToList(),
		PlayersData = Profiles.active.Select( ConvertDuckGameProfileToPlayerData ).ToList(),
		Winner = ConvertDuckGameTeamToTeamData( Teams.winning.FirstOrDefault() ),
		TimeEnded = DateTime.Now,
	} );

	internal void StartRecordingRound() => MessageHandler?.SendMessage( new StartRoundMessage()
	{
		LevelName = Level.current.level,
		Teams = Teams.active.Select( ConvertDuckGameTeamToTeamData ).ToList(),
		Players = Profiles.activeNonSpectators.Select( GetPlayerID ).ToList(),
		TimeStarted = DateTime.Now,
	} );

	internal void StopRecordingRound() => MessageHandler?.SendMessage( new EndRoundMessage()
	{
		Teams = Teams.active.Select( ConvertDuckGameTeamToTeamData ).ToList(),
		Players = Profiles.activeNonSpectators.Select( GetPlayerID ).ToList(),
		Winner = ConvertDuckGameTeamToTeamData( GameMode.lastWinners.FirstOrDefault()?.team ),
		TimeEnded = DateTime.Now,
	} );

	internal void TrackKill( Duck duckVictim, DestroyType type, bool isNetworkMessage )
	{
		Profile killerProfile = null;

		var objectResponsible = string.Empty;

		if( type != null )
		{
			var kv = GetBestDestroyTypeKillerAndWeapon( type );
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
			killerTeamData = ConvertDuckGameProfileToTeamData( killerProfile );
		}

		var killData = new KillData()
		{
			Killer = killerTeamData,
			Victim = ConvertDuckGameProfileToTeamData( duckVictim.profile ),
			DeathTypeClassName = type?.GetType()?.Name,
			TimeOccured = DateTime.Now,
			ObjectClassName = objectResponsible
		};

		MessageHandler?.SendMessage( new TrackKillMessage()
		{
			KillData = killData
		} );
	}

	internal void CollectObjectData()
	{
		var collectObjectDataMessage = new CollectObjectDataMessage()
		{
			ObjectDataList = new List<ObjectData>()
		};

		//foreach( var type in DuckGame.All )

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

	private static KeyValuePair<Profile, string> GetBestDestroyTypeKillerAndWeapon( DestroyType destroyType )
	{
		//try a direct check, easiest one
		Profile profile = destroyType.responsibleProfile;

		string weapon = string.Empty;

		if( destroyType is DTShot shotType && shotType.bulletFiredFrom != null )
		{
			//god, grenade launchers are a pain in the ass
			var type = shotType.bulletFiredFrom.GetType();

			if( shotType.bulletFiredFrom.killThingType != null )
			{
				type = shotType.bulletFiredFrom.killThingType;
			}

			if( shotType.bulletFiredFrom.responsibleProfile != null )
			{
				profile = destroyType.responsibleProfile;
			}

			weapon = type.Name;
		}

		//... I know I know, but either I Import the tuples nuget or I make my own struct, so whatever
		return new KeyValuePair<Profile, string>( profile, weapon );
	}

	private static TeamData ConvertDuckGameTeamToTeamData( Team duckgameteam )
	{
		return duckgameteam is null ? null : new TeamData()
		{
			HasHat = duckgameteam.hasHat,
			Score = duckgameteam.score,
			HatName = duckgameteam.name,
			IsCustomHat = duckgameteam.customData != null,
			Players = duckgameteam.activeProfiles.Select( x => GetPlayerID( x ) ).ToList()
		};
	}

	private static PlayerData ConvertDuckGameProfileToPlayerData( Profile profile )
	{
		return new PlayerData()
		{
			Name = profile.name,
			UserId = GetPlayerID( profile ),
		};
	}

	private static TeamData ConvertDuckGameProfileToTeamData( Profile profile )
	{
		var teamData = ConvertDuckGameTeamToTeamData( profile.team );

		if( teamData != null )
		{
			teamData.Players = teamData.Players.Where( x => x.Equals( GetPlayerID( profile ), StringComparison.InvariantCultureIgnoreCase ) ).ToList();
		}

		return teamData;
	}

	private static string GetPlayerID( Profile profile )
	{
		var id = profile.id;

		if( Network.isActive )
		{
			var steamid = profile.steamID.ToString();

			id = steamid;

			if( profile.isRemoteLocalDuck )
			{
				id = $"{steamid}_{profile.name}";
			}
		}

		return id;
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
