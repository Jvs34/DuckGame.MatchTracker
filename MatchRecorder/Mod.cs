using System.Reflection;
using Harmony;

namespace MatchRecorder
{
	public class Mod : DuckGame.DisabledMod
	{
		public static MatchRecorderHandler Recorder { get; private set; }

		protected override void OnPreInitialize()
		{
#if DEBUG
			System.Diagnostics.Debugger.Launch();
#endif
			Recorder = new MatchRecorderHandler( configuration.directory );

			//HUD.AddInputChangeDisplay
			HarmonyInstance.Create( "MatchRecorder" ).PatchAll( Assembly.GetExecutingAssembly() );
		}

		protected override void OnPostInitialize()
		{
		}
	}
}
