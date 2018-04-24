using System.Reflection;
using Harmony;

namespace MatchRecorder
{
	public class Mod : DuckGame.Mod
	{
		protected override void OnPreInitialize()
		{
#if false
			System.Diagnostics.Debugger.Launch();
#endif

			HarmonyInstance.Create( "MatchRecorder" ).PatchAll( Assembly.GetExecutingAssembly() );
		}
	}
}
