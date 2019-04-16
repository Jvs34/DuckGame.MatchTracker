using Harmony;
using System;
using System.IO;
using System.Reflection;

namespace MatchRecorder
{
	public class MatchRecorderMod : DuckGame.DisabledMod
	{
		public static MatchRecorderHandler Recorder { get; set; }

		public MatchRecorderMod()
		{
#if DEBUG
			System.Diagnostics.Debugger.Launch();
#endif
			AppDomain.CurrentDomain.AssemblyResolve += ModResolve;
		}

		~MatchRecorderMod()
		{
			AppDomain.CurrentDomain.AssemblyResolve -= ModResolve;
		}

		protected override void OnPreInitialize()
		{
			Recorder = new MatchRecorderHandler( configuration.directory );
			HarmonyInstance.Create( "MatchRecorder" ).PatchAll( Assembly.GetExecutingAssembly() );
		}

		private Assembly ModResolve( object sender , ResolveEventArgs args )
		{
			string cleanName = args.Name.Split( ',' ) [0];
			//now try to load the requested assembly

			string folder = "Release";
#if DEBUG
			folder = "Debug";
#endif
			//TODO: find a better way to output this stuff
			string assemblyFolder = Path.Combine( configuration.directory , "MatchRecorder" , "bin" , "x86" , folder , "net471" );
			string assemblyPath = Path.GetFullPath( Path.Combine( assemblyFolder , cleanName + ".dll" ) );

			byte [] assemblyBytes = File.ReadAllBytes( assemblyPath );

			return Assembly.Load( assemblyBytes );
		}
	}
}