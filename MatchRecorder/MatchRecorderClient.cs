using DuckGame;
using HarmonyLib;
using MatchRecorderShared;
using MatchRecorderShared.Messages;
using MatchTracker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace MatchRecorder
{
	public class MatchRecorderClient
	{
		public string ModPath { get; }
		private Process RecorderProcess { get; set; }
		private ClientMessageHandler MessageHandler { get; }
		private Task MessageHandlerTask { get; set; }
		public string RecorderUrl { get; set; } = "http://localhost:6969";
		private HttpClient HttpClient { get; }

		public MatchRecorderClient( string directory )
		{
			HttpClient = new HttpClient()
			{
				BaseAddress = new Uri( RecorderUrl )
			};

			ModPath = directory;
			MessageHandler = new ClientMessageHandler( HttpClient );
		}

		public static void ShowHUDMessage( string text , float lifetime = 1f )
		{
			var cornerMessage = HUD.AddCornerMessage( HUDCorner.TopLeft , text );
			cornerMessage.slide = 1;
			cornerMessage.willDie = true;
			cornerMessage.life = lifetime;
		}

		internal void Update()
		{
			CheckRecorderProcess();

			if( ( MessageHandlerTask is null || MessageHandlerTask.IsCompleted == true ) && MessageHandler != null )
			{
				System.Diagnostics.Debug.WriteLine( "Starting MessageHandler task" );
				MessageHandlerTask = Task.Run( async () =>
				{
					try
					{
						await MessageHandler.ThreadedLoop();
					}
					catch( Exception e )
					{
						System.Diagnostics.Debug.WriteLine( e );
					}
				} );
			}
		}

		private void CheckRecorderProcess()
		{
			if( RecorderProcess is null || RecorderProcess.HasExited )
			{
				RecorderProcess = Process.Start( new ProcessStartInfo()
				{
					FileName = Path.Combine( ModPath , @"MatchRecorderOOP\bin\Debug\net7.0\MatchRecorderOOP.exe" ) ,
					WorkingDirectory = ModPath ,
					CreateNoWindow = false ,
					Arguments = $"--urls {RecorderUrl} --{nameof( RecorderSettings.RecorderType )} OBSMergedVideo --{nameof( RecorderSettings.RecordingEnabled )} true --{nameof( RecorderSettings.DuckGameProcessID )} {Process.GetCurrentProcess().Id}" ,
				} );
			}
		}

		internal void StartRecordingRound()
		{
			MessageHandler?.SendMessage( new StartRoundMessage()
			{
				LevelName = Level.current.level ,
				Teams = Teams.active.Select( x => ConvertDuckGameTeamToTeamData( x ) ).ToList() ,
				Players = Profiles.activeNonSpectators.Select( x => GetPlayerID( x ) ).ToList() ,
			} );
		}

		internal void StopRecordingRound()
		{
			MessageHandler?.SendMessage( new EndRoundMessage()
			{
				Teams = Teams.active.Select( x => ConvertDuckGameTeamToTeamData( x ) ).ToList() ,
				Players = Profiles.activeNonSpectators.Select( x => GetPlayerID( x ) ).ToList() ,
				Winner = ConvertDuckGameTeamToTeamData( GameMode.lastWinners.FirstOrDefault()?.team ) ,
			} );
		}

		internal void StartRecordingMatch()
		{
			MessageHandler?.SendMessage( new StartMatchMessage()
			{
				Teams = Teams.active.Select( x => ConvertDuckGameTeamToTeamData( x ) ).ToList() ,
				Players = Profiles.activeNonSpectators.Select( x => GetPlayerID( x ) ).ToList() ,
				PlayersData = Profiles.activeNonSpectators.Select( x => ConvertDuckGameProfileToPlayerData( x ) ).ToList() ,
			} );
		}

		internal void StopRecordingMatch()
		{
			MessageHandler?.SendMessage( new EndMatchMessage()
			{
				Teams = Teams.active.Select( x => ConvertDuckGameTeamToTeamData( x ) ).ToList() ,
				Players = Profiles.activeNonSpectators.Select( x => GetPlayerID( x ) ).ToList() ,
				PlayersData = Profiles.activeNonSpectators.Select( x => ConvertDuckGameProfileToPlayerData( x ) ).ToList() ,
				Winner = ConvertDuckGameTeamToTeamData( Teams.winning.FirstOrDefault() ) ,
			} );
		}

		private TeamData ConvertDuckGameTeamToTeamData( Team duckgameteam )
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

		private PlayerData ConvertDuckGameProfileToPlayerData( Profile profile )
		{
			return new PlayerData()
			{
				Name = profile.name ,
				UserId = GetPlayerID( profile ) ,
			};
		}

		private string GetPlayerID( Profile profile )
		{
			return Network.isActive ? profile.steamID.ToString() : profile.id;
		}

	}

	#region HOOKS
	//save the video and stop recording
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

	//start recording a round
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
	internal static class UpdateLoop
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

	#endregion HOOKS
}