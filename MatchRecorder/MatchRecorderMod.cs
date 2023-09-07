using HarmonyLib;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Reflection;

namespace MatchRecorder
{
	public class MatchRecorderMod : DuckGame.ClientMod
	{
		public static MatchRecorderMod Instance => DuckGame.ModLoader.GetMod<MatchRecorderMod>();
		public MatchRecorderClient Recorder { get; set; }
		private Harmony HarmonyInstance { get; set; }

		public MatchRecorderMod()
		{
			HarmonyInstance = new Harmony( GetType().Namespace );
		}

		protected override void OnPostInitialize()
		{
#if DEBUG
			System.Diagnostics.Debugger.Launch();
#endif
			HarmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );
			Recorder = new MatchRecorderClient( configuration.directory );
		}

	}
}