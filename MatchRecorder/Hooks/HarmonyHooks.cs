using DuckGame;
using HarmonyLib;

namespace MatchRecorder.Hooks;

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable IDE0059 // Unnecessary assignment of a value

//a round is done after a level change in all cases
[HarmonyPatch( typeof( Level ), "set_current" )]
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
			MatchRecorderMod.Instance.Recorder.StopRecordingMatch( true );
		}
	}
}

//start recording a round after the virtual transition, it'd be annoying in recordings otherwise
[HarmonyPatch( typeof( VirtualTransition ), nameof( VirtualTransition.GoUnVirtual ) )]
internal static class VirtualTransition_GoUnVirtual
{
	private static void Postfix()
	{
		if( MatchRecorderMod.Instance is null || MatchRecorderMod.Instance.Recorder is null )
		{
			return;
		}

		//only bother if the current level is something we care about

		if( Level.current is GameLevel gameLevel )
		{
			MatchRecorderMod.Instance.Recorder.StartRecordingRound();
			MatchRecorderMod.Instance.Recorder.CollectLevelData( gameLevel.data, true );
		}
	}
}

//this is called once when a new match starts, but not if you're a client and in multiplayer and the host decides to continue from the endgame screen instead of going
//back to lobby first
[HarmonyPatch( typeof( Main ), "ResetMatchStuff" )]
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


[HarmonyPatch( typeof( Level ), nameof( Level.UpdateCurrentLevel ) )]
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
[HarmonyPatch( typeof( NMKillDuck ), nameof( NMKillDuck.Activate ) )]
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
[HarmonyPatch( typeof( Duck ), nameof( Duck.Kill ) )]
internal static class Duck_Kill
{
	private static void Prefix( Duck __instance, DestroyType type, bool __state )
	{
		if( MatchRecorderMod.Instance is null || MatchRecorderMod.Instance.Recorder is null )
		{
			return;
		}

		//to check whether this is the first time Duck.Kill was called, let's save the current Duck.forceDead
		__state = __instance.forceDead;
	}

	private static void Postfix( Duck __instance, DestroyType type, bool __state, bool __result )
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

		MatchRecorderMod.Instance.Recorder.TrackKill( __instance, type, __instance.isKillMessage );
	}
}

//TODO: undecided about this one, I don't see much point in doing this and also
//the missing references makes this annoying to do with attributes
//[HarmonyPatch( typeof( MonoMain ) , nameof( MonoMain.KillEverything ) )]
//internal static class MonoMain_KillEverything
//{
//	private static void Postfix( MonoMain __instance )
//	{
//		MatchRecorderMod.Instance?.Dispose();
//		MatchRecorderMod.Instance = null;
//	}
//}
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0059 // Unnecessary assignment of a value