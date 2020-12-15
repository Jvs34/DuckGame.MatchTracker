﻿using DuckGame;
using HarmonyLib;
using MatchRecorderShared;
using MatchRecorderShared.Messages;
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
		private Process RecorderProcess { get; set; }
		private MessageHandler MessageHandler { get; }
		private Task MessageHandlerTask { get; set; }
		public MatchRecorderClient( string directory )
		{
			ModPath = directory;
			MessageHandler = new MessageHandler( false );
			MessageHandler.OnReceiveMessage += OnReceiveMessage;
		}

		private void OnReceiveMessage( BaseMessage message )
		{
			if( message is ShowHUDTextMessage hudtext )
			{
				var cornerMessage = DuckGame.HUD.AddCornerMessage( DuckGame.HUDCorner.TopLeft , message );
				cornerMessage.slide = 1;
				cornerMessage.willDie = true;
				cornerMessage.life = 1;
				
			}
		}

		internal void Update()
		{
			CheckRecorderProcess();

			if( ( MessageHandlerTask is null || MessageHandlerTask.IsCompleted == true ) && MessageHandler != null )
			{
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

			MessageHandler?.CheckMessages();
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

		internal void StartRecordingRound()
		{
			MessageHandler?.SendMessage( new StartRoundMessage()
			{
				Level = Level.current.level ,
			} );
		}

		internal void StopRecordingRound()
		{

		}

		internal void StartRecordingMatch()
		{

		}

		internal void StopRecordingMatch()
		{

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

#pragma warning restore IDE0051 // Remove unused private members
	#endregion HOOKS
}