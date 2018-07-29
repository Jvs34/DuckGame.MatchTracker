﻿using System;
using System.IO;
using System.Reflection;
using Harmony;

namespace MatchRecorder
{
	public class MatchRecorderMod : DuckGame.DisabledMod
	{
		public static MatchRecorderHandler Recorder { get; private set; }

		public MatchRecorderMod()
		{
			AppDomain.CurrentDomain.AssemblyResolve += ModResolve;
		}

		~MatchRecorderMod()
		{
			AppDomain.CurrentDomain.AssemblyResolve -= ModResolve;
		}

		protected override void OnPreInitialize()
		{
#if RELEASE
			System.Diagnostics.Debugger.Launch();
#endif

			Recorder = new MatchRecorderHandler( configuration.directory );
			HarmonyInstance.Create( "MatchRecorder" ).PatchAll( Assembly.GetExecutingAssembly() );
		}

		private Assembly ModResolve( object sender , ResolveEventArgs args )
		{
			String cleanName = args.Name.Split( ',' ) [0];
			//now try to load the requested assembly

			String folder = "Release";
#if DEBUG
			folder = "Debug";
#endif

			String assemblyFolder = Path.Combine( configuration.directory , "MatchRecorder" , "bin" , "x86" , folder , "net471" );
			String assemblyPath = Path.GetFullPath( Path.Combine( assemblyFolder , cleanName + ".dll" ) );
			return Assembly.LoadFile( assemblyPath );
		}
	}
}