﻿using DuckGame;
using HarmonyLib;
using System.Reflection;

namespace MatchRecorder.Companion;

public class MatchRecorderCompanionMod : ClientMod //Mod
{
	private Harmony HarmonyInstance { get; set; }

	public MatchRecorderCompanionMod() => HarmonyInstance = new Harmony( GetType().Namespace );

	protected override void OnPostInitialize() => HarmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );
}

#region HOOKS

internal static class Duck_Kill
{
	public static void Postfix( Duck ___instance, DestroyType destroyType )
	{

	}
}

internal static class NMKillDuck_OnSerialize
{
	private static void Postfix( NMKillDuck ___instance )
	{
		var buffer = ___instance.serializedData;

	}
}

internal static class NMKillDuck_OnDeserialize
{
	private static void Postfix( NMKillDuck ___instance, BitBuffer buffer )
	{
	}
}

#endregion HOOKS
