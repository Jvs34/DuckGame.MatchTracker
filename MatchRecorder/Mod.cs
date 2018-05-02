using System.Reflection;
using Harmony;

namespace MatchRecorder
{
	public class Mod : DuckGame.Mod
	{
		private static MatchRecorderHandler matchRecorderSingleton = null;

		public static MatchRecorderHandler Recorder
		{
			get => matchRecorderSingleton;
		}

		protected override void OnPreInitialize()
		{
#if DEBUG
			System.Diagnostics.Debugger.Launch();
#endif
			matchRecorderSingleton = new MatchRecorderHandler( configuration.directory );

			//HUD.AddInputChangeDisplay
			HarmonyInstance.Create( "MatchRecorder" ).PatchAll( Assembly.GetExecutingAssembly() );

		}


		//TODO:uhhhhh find a better place to start this, there has to be hook for when the game is fully initialized
		protected override void OnPostInitialize()
		{
			Recorder?.Init();
		}
	}
}
