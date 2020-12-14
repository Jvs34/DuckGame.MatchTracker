using DuckGame;
using HarmonyLib;
using MatchRecorderShared;
using Ninja.WebSockets;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace MatchRecorder
{
	public class MatchRecorderClient
	{
		public string ModPath { get; }
		public WebSocketClientFactory WebSocketFactory { get; } = new WebSocketClientFactory();
		private WebsocketHandler WebSocketHandler { get; set; }
		private Task<WebSocket> WebSocketConnectionTask { get; set; }
		private Process RecorderProcess { get; set; }

		public MatchRecorderClient( string directory ) => ModPath = directory;

		internal void Update()
		{
			CheckRecorderProcess();

			if( WebSocketHandler?.IsClosed == true )
			{
				WebSocketHandler.Dispose();
				WebSocketHandler = null;
			}

			if( WebSocketConnectionTask?.IsCompleted == true )
			{
				if( WebSocketConnectionTask.Status == TaskStatus.RanToCompletion )
				{
					WebSocketHandler = new WebsocketHandler( WebSocketConnectionTask.Result );
				}

				WebSocketConnectionTask = null;
			}


			//start connecting now
			if( WebSocketConnectionTask is null && WebSocketHandler is null )
			{
				WebSocketConnectionTask = WebSocketFactory.ConnectAsync( new Uri( "ws://127.0.0.1:6969" ) , new WebSocketClientOptions()
				{
					KeepAliveInterval = TimeSpan.FromSeconds( 1 ) ,
				} );
			}

			
			WebSocketHandler?.UpdateLoop().Wait();
		}

		private void CheckRecorderProcess()
		{
			if( RecorderProcess is null || RecorderProcess.HasExited )
			{
				RecorderProcess = Process.Start( new ProcessStartInfo()
				{
					FileName = Path.Combine( ModPath , @"MatchRecorderOOP\bin\Debug\net5.0\MatchRecorderOOP.exe" ) ,
					WorkingDirectory = ModPath ,
					CreateNoWindow = false ,
					//TODO: Arguments
				} );
			}
		}
	}

	#region HOOKS
#pragma warning disable IDE0051 // Remove unused private members
	//save the video and stop recording
	[HarmonyPatch( typeof( Level ) , "set_current" )]
	internal static class Level_SetCurrent
	{
		//changed the Postfix to a Prefix so we can get the Level.current before it's changed to the new one
		//as we use it to check if the nextlevel is going to be a GameLevel if this one is a RockScoreboard, then we try collecting matchdata again
		private static void Prefix( Level value )
		{
			if( MatchRecorderMod.Instance is null || MatchRecorderMod.Instance.Recorder is null )
			{
				return;
			}

			//regardless if the current level can be recorded or not, we're done with the current round recording so just save and stop
			/*
			if( MatchRecorderMod.Instance.Recorder.IsRecordingRound == true )
			{
				MatchRecorderMod.Instance.Recorder.StopRecordingRound();
			}

			//seems like we launched a match just now, start recording
			if( MatchRecorderMod.Instance.Recorder.IsLevelRecordable( value ) == true && MatchRecorderMod.Instance.Recorder.IsRecordingMatch == false )
			{
				MatchRecorderMod.Instance.Recorder.IsRecordingMatch = true;
				MatchRecorderMod.Instance.Recorder.StartRecordingMatch();
			}
			*/
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

			/*
			if( MatchRecorderMod.Instance.Recorder.IsRecordingMatch == true )
			{
				MatchRecorderMod.Instance.Recorder.IsRecordingMatch = false;
				MatchRecorderMod.Instance.Recorder.StopRecordingMatch();
			}
			*/
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
			/*
			if( MatchRecorderMod.Instance.Recorder.IsLevelRecordable( Level.current ) )
			{
				MatchRecorderMod.Instance.Recorder.StartRecordingRound();
				MatchRecorderMod.Instance.Recorder.GatherLevelData( Level.current );
			}
			*/
		}
	}

#pragma warning restore IDE0051 // Remove unused private members
	#endregion HOOKS
}