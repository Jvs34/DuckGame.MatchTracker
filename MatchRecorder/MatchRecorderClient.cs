using DuckGame;
using HarmonyLib;
using System;

namespace MatchRecorder
{
	public class MatchRecorderClient
	{
		public string ModPath { get; }

		public MatchRecorderClient( string directory )
		{
			ModPath = directory;
		}

		internal void Update()
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