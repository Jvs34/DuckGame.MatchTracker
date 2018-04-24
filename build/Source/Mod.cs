using System.Reflection;
using Harmony;

namespace MatchRecorder
{
	public class Mod : DuckGame.Mod
	{
		public static MatchRecorderHandler matchRecorderSingleton = null;

		protected override void OnPreInitialize()
		{
#if false
			System.Diagnostics.Debugger.Launch();
#endif
			matchRecorderSingleton = new MatchRecorderHandler();

			//HUD.AddInputChangeDisplay
			HarmonyInstance.Create( "MatchRecorder" ).PatchAll( Assembly.GetExecutingAssembly() );

		}
	}
}
