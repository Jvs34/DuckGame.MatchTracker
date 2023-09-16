using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MatchRecorderCompanion
{
	public class MatchRecorderCompanionMod : DuckGame.Mod
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
}
