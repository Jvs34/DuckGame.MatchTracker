using System.Reflection;
using Harmony;

namespace MatchRecorder
{
	public class Mod : DuckGame.Mod
	{
		private static MatchRecorderHandler matchRecorderSingleton = null;

		protected override void OnPreInitialize()
		{

#if true
			System.Diagnostics.Debugger.Launch();
#endif
			matchRecorderSingleton = new MatchRecorderHandler();

			//HUD.AddInputChangeDisplay
			HarmonyInstance.Create( "MatchRecorder" ).PatchAll( Assembly.GetExecutingAssembly() );

		}

		public static MatchRecorderHandler GetRecorder()
		{
			return matchRecorderSingleton;
		}


		//uhhhhh find a better place to start this, there has to be hook for when the game is fully initialized
		protected override void OnPostInitialize()
		{
			
			GetRecorder().Init();
		}
	}
}
