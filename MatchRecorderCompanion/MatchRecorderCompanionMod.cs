using DuckGame;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MatchRecorderCompanion
{
	public class MatchRecorderCompanionMod : DuckGame.ClientMod
	{
		private Harmony HarmonyInstance { get; set; }

		public MatchRecorderCompanionMod()
		{
			HarmonyInstance = new Harmony( GetType().Namespace );
		}

		protected override void OnPostInitialize()
		{
			HarmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );
		}
	}

	#region HOOKS

	internal static class Duck_Kill
	{
		public static void Postfix( Duck ___instance , DestroyType destroyType )
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
		private static void Postfix( NMKillDuck ___instance , BitBuffer buffer )
		{
		}
	}

	#endregion HOOKS
}
