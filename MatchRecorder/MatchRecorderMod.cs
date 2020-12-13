using HarmonyLib;
using System;
using System.IO;
using System.Reflection;

namespace MatchRecorder
{
	public class MatchRecorderMod : DuckGame.DisabledMod
	{
		public static MatchRecorderMod Instance => DuckGame.ModLoader.GetMod<MatchRecorderMod>();
		public MatchRecorderClient Recorder { get; set; }
		private Harmony HarmonyInstance { get; set; }

		static MatchRecorderMod()
		{
#if DEBUG
			System.Diagnostics.Debugger.Launch();
#endif
			AppDomain.CurrentDomain.AssemblyResolve += ModResolveStatic;
		}

		protected override void OnPostInitialize()
		{
			HarmonyInstance = new Harmony( GetType().Namespace );
			HarmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );
			Recorder = new MatchRecorderClient( configuration.directory );
		}

		private static Assembly ModResolveStatic( object sender , ResolveEventArgs args ) => Instance?.ModResolveInstance( sender , args );

		private Assembly ModResolveInstance( object sender , ResolveEventArgs args )
		{
			return null;

			string dllFolder = Path.GetFileNameWithoutExtension( GetType().Assembly.ManifestModule?.ScopeName ?? GetType().Namespace );
			string cleanName = args.Name.Split( ',' ) [0];
			//now try to load the requested assembly

			//TODO: check if there's any way to obtain this path in a better way
			string assemblyFolder = Path.Combine( configuration.directory , dllFolder , "Output" , "net471" );
			string assemblyPath = Path.GetFullPath( Path.Combine( assemblyFolder , cleanName + ".dll" ) );

			if( File.Exists( assemblyPath ) )
			{
				byte [] assemblyBytes = File.ReadAllBytes( assemblyPath );

				return Assembly.Load( assemblyBytes );
			}

			return null;
		}

	}
}