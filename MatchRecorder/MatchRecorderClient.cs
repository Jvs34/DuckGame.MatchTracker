using DuckGame;
using HarmonyLib;
using MatchRecorderShared;
using MatchRecorderShared.Enums;
using MatchRecorderShared.Messages;
using MatchTracker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MatchRecorder
{
	public sealed class MatchRecorderClient : IDisposable
	{
		private bool IsDisposed { get; set; }
		private CancellationTokenSource StopTokenSource { get; }
		private CancellationToken StopToken { get; } = CancellationToken.None;
		public string ModPath { get; }
		private Process RecorderProcess { get; set; }
		private ClientMessageHandler MessageHandler { get; }
		private Task MessageHandlerTask { get; set; }
		public string RecorderUrl { get; set; } = "http://localhost:6969";
		private HttpClient HttpClient { get; }
		private ModSettings Settings { get; set; } = new ModSettings();
		private JsonSerializer Serializer { get; } = new JsonSerializer()
		{
			Formatting = Formatting.Indented
		};
		private string SettingsPath { get; }

		public MatchRecorderClient( string directory )
		{
			ModPath = directory;
			SettingsPath = Path.Combine( ModPath , "modsettings.json" );
			StopTokenSource = new CancellationTokenSource();
			StopToken = CancellationToken.None;
			HttpClient = new HttpClient()
			{
				BaseAddress = new Uri( RecorderUrl ) ,
				Timeout = TimeSpan.FromSeconds( 10 )
			};
			HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd( "duckgame-matchrecorder/1.0" );

			MessageHandler = new ClientMessageHandler( HttpClient , StopToken );
			MessageHandler.OnReceiveMessage += OnReceiveMessage;

			LoadSettings();
			SaveSettings();
		}

		public void LoadSettings()
		{
			using var dataStream = File.Open( SettingsPath , FileMode.Open );
			using var reader = new StreamReader( dataStream );
			using var jsonReader = new JsonTextReader( reader );
			Settings = Serializer.Deserialize<ModSettings>( jsonReader );
		}

		public void SaveSettings()
		{
			using var writer = File.CreateText( SettingsPath );
			Serializer.Serialize( writer , Settings );
		}

		private void OnReceiveMessage( TextMessage message )
		{
			ShowHUDMessage( message.Message );
		}

		public static void ShowHUDMessage( string text , float lifetime = 1f , TextMessagePosition position = TextMessagePosition.TopLeft )
		{
			var cornerMessage = HUD.AddCornerMessage( (HUDCorner) position , text , true );
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

			CheckRecorderProcess();
			MessageHandler.UpdateMessages();

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
			} , StopToken );
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
				FileName = Path.Combine( ModPath , "MatchRecorderOOP" , "MatchRecorderOOP.exe" ) ,
				WorkingDirectory = ModPath ,
				UseShellExecute = false ,
				CreateNoWindow = false ,
				WindowStyle = ProcessWindowStyle.Minimized ,
			};

			var envVars = startInfo.EnvironmentVariables;
			envVars.Add( "ASPNETCORE_URLS" , $"{RecorderUrl}" );
			envVars.Add( nameof( IRecorderSharedSettings.RecorderType ) , $"{Settings.RecorderType}" );
			envVars.Add( nameof( IRecorderSharedSettings.RecordingEnabled ) , $"{Settings.RecordingEnabled}" );
			envVars.Add( nameof( RecorderSettings.DuckGameProcessID ) , $"{Process.GetCurrentProcess().Id}" );
			envVars.Add( nameof( RecorderSettings.AutoCloseWhenParentDies ) , $"{true}" );

			RecorderProcess = Process.Start( startInfo );
		}

		#region MATCHTRACKING
		internal void StartRecordingMatch() => MessageHandler?.SendMessage( new StartMatchMessage()
		{
			Teams = Teams.active.Select( ConvertDuckGameTeamToTeamData ).ToList() ,
			Players = Profiles.activeNonSpectators.Select( GetPlayerID ).ToList() ,
			PlayersData = Profiles.active.Select( ConvertDuckGameProfileToPlayerData ).ToList() ,
			TimeStarted = DateTime.Now ,
		} );

		internal void StopRecordingMatch() => MessageHandler?.SendMessage( new EndMatchMessage()
		{
			Teams = Teams.active.Select( ConvertDuckGameTeamToTeamData ).ToList() ,
			Players = Profiles.activeNonSpectators.Select( GetPlayerID ).ToList() ,
			PlayersData = Profiles.active.Select( ConvertDuckGameProfileToPlayerData ).ToList() ,
			Winner = ConvertDuckGameTeamToTeamData( Teams.winning.FirstOrDefault() ) ,
			TimeEnded = DateTime.Now ,
		} );

		internal void StartRecordingRound() => MessageHandler?.SendMessage( new StartRoundMessage()
		{
			LevelName = Level.current.level ,
			Teams = Teams.active.Select( ConvertDuckGameTeamToTeamData ).ToList() ,
			Players = Profiles.activeNonSpectators.Select( GetPlayerID ).ToList() ,
			TimeStarted = DateTime.Now ,
		} );

		internal void StopRecordingRound() => MessageHandler?.SendMessage( new EndRoundMessage()
		{
			Teams = Teams.active.Select( ConvertDuckGameTeamToTeamData ).ToList() ,
			Players = Profiles.activeNonSpectators.Select( GetPlayerID ).ToList() ,
			Winner = ConvertDuckGameTeamToTeamData( GameMode.lastWinners.FirstOrDefault()?.team ) ,
			TimeEnded = DateTime.Now ,
		} );

		internal void TrackKill( Duck duckVictim , DestroyType type , bool isNetworkMessage )
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
				Killer = killerTeamData ,
				Victim = ConvertDuckGameProfileToTeamData( duckVictim.profile ) ,
				DeathTypeClassName = type?.GetType()?.Name ,
				TimeOccured = DateTime.Now ,
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

		internal void CollectLevelData()
		{


		}

		private static KeyValuePair<Profile , string> GetBestDestroyTypeKillerAndWeapon( DestroyType destroyType )
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
			return new KeyValuePair<Profile , string>( profile , weapon );
		}

		private static TeamData ConvertDuckGameTeamToTeamData( Team duckgameteam )
		{
			return duckgameteam is null ? null : new TeamData()
			{
				HasHat = duckgameteam.hasHat ,
				Score = duckgameteam.score ,
				HatName = duckgameteam.name ,
				IsCustomHat = duckgameteam.customData != null ,
				Players = duckgameteam.activeProfiles.Select( x => GetPlayerID( x ) ).ToList()
			};
		}

		private static PlayerData ConvertDuckGameProfileToPlayerData( Profile profile )
		{
			return new PlayerData()
			{
				Name = profile.name ,
				UserId = GetPlayerID( profile ) ,
			};
		}

		private static TeamData ConvertDuckGameProfileToTeamData( Profile profile )
		{
			var teamData = ConvertDuckGameTeamToTeamData( profile.team );

			if( teamData != null )
			{
				teamData.Players = teamData.Players.Where( x => x.Equals( GetPlayerID( profile ) , StringComparison.InvariantCultureIgnoreCase ) ).ToList();
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

	#region HOOKS
#pragma warning disable IDE0051 // Remove unused private members

	//a round is done after a level change in all cases
	[HarmonyPatch( typeof( Level ) , "set_current" )]
	internal static class Level_SetCurrent
	{
		private static void Postfix()
		{
			if( MatchRecorderMod.Instance is null || MatchRecorderMod.Instance.Recorder is null )
			{
				return;
			}

			//regardless if the current level can be recorded or not, we're done with the current round recording so just save and stop
			MatchRecorderMod.Instance.Recorder.StopRecordingRound();

			if( Level.current is GameLevel )
			{
				MatchRecorderMod.Instance.Recorder.StartRecordingMatch();
			}

			if( Level.current is TitleScreen )
			{
				MatchRecorderMod.Instance.Recorder.StopRecordingMatch();
			}
		}
	}

	//start recording a round after the virtual transition, it'd be annoying in recordings otherwise
	[HarmonyPatch( typeof( VirtualTransition ) , nameof( VirtualTransition.GoUnVirtual ) )]
	internal static class VirtualTransition_GoUnVirtual
	{
		private static void Postfix()
		{
			if( MatchRecorderMod.Instance is null || MatchRecorderMod.Instance.Recorder is null )
			{
				return;
			}

			//only bother if the current level is something we care about

			if( Level.current is GameLevel )
			{
				MatchRecorderMod.Instance.Recorder.StartRecordingRound();
			}
		}
	}

	//this is called once when a new match starts, but not if you're a client and in multiplayer and the host decides to continue from the endgame screen instead of going
	//back to lobby first
	[HarmonyPatch( typeof( Main ) , "ResetMatchStuff" )]
	internal static class Main_ResetMatchStuff
	{
		private static void Prefix()
		{
			if( MatchRecorderMod.Instance is null || MatchRecorderMod.Instance.Recorder is null )
			{
				return;
			}

			MatchRecorderMod.Instance.Recorder.StopRecordingMatch();
		}
	}


	[HarmonyPatch( typeof( Level ) , nameof( Level.UpdateCurrentLevel ) )]
	internal static class Level_UpdateCurrentLevel
	{
		private static void Prefix()
		{
			if( MatchRecorderMod.Instance is null || MatchRecorderMod.Instance.Recorder is null )
			{
				return;
			}

			MatchRecorderMod.Instance.Recorder.Update();
		}
	}

	//sets a global networkconnection to allow TrackKill to find out who sent the kill message
	[HarmonyPatch( typeof( NMKillDuck ) , nameof( NMKillDuck.Activate ) )]
	internal static class NMKillDuck_Activate
	{
		internal static NetworkConnection CurrentNMKillDuckConnection { get; private set; }

		private static void Prefix( NMKillDuck __instance )
		{
			CurrentNMKillDuckConnection = __instance.connection;
		}

		private static void Postfix( NMKillDuck __instance )
		{
			CurrentNMKillDuckConnection = null;
		}
	}

	//try to track a kill whether it's a networked one or not
	[HarmonyPatch( typeof( Duck ) , nameof( Duck.Kill ) )]
	internal static class Duck_Kill
	{
		private static void Prefix( Duck __instance , DestroyType type , bool __state )
		{
			if( MatchRecorderMod.Instance is null || MatchRecorderMod.Instance.Recorder is null )
			{
				return;
			}

			//to check whether this is the first time Duck.Kill was called, let's save the current Duck.forceDead
			__state = __instance.forceDead;
		}

		private static void Postfix( Duck __instance , DestroyType type , bool __state , bool __result )
		{
			if( MatchRecorderMod.Instance is null || MatchRecorderMod.Instance.Recorder is null || !__result )
			{
				return;
			}

			//some things like fire/flaregun keep calling Duck.Kill even after death, so ignore the duplicate calls
			if( __state && __state == __instance.forceDead )
			{
				return;
			}

			MatchRecorderMod.Instance.Recorder.TrackKill( __instance , type , __instance.isKillMessage );
		}
	}

	//[HarmonyPatch( typeof( MonoMain ) , nameof( MonoMain.KillEverything ) )]
	internal static class MonoMain_KillEverything
	{
		private static void Postfix( MonoMain __instance )
		{
			MatchRecorderMod.Instance?.Dispose();
		}
	}
#pragma warning restore IDE0051 // Remove unused private members
	#endregion HOOKS
}