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

#if DISCORDRECORDER
		private void CopyDiscordSharpPlusDependencies( string modPath )
		{
			var duckgameDir = Directory.GetCurrentDirectory();

			foreach( var fileName in MatchDiscordRecorder.DiscordRecorder.DSharpRequirements )
			{
				//can't exactly do it any other way, duck game mods are loaded by copying the dll into memory
				var thirdPartyFile = Path.Combine( modPath , "ThirdParty" , fileName );
				var outputFile = Path.Combine( duckgameDir , fileName );

				if( File.Exists( thirdPartyFile ) && !File.Exists( outputFile ) )
				{
					File.Copy( thirdPartyFile , outputFile );
				}

			}
		}
#endif

		~MatchRecorderMod()
		{
			AppDomain.CurrentDomain.AssemblyResolve -= ModResolve;
		}

		protected override void OnPreInitialize()
		{
#if DISCORDRECORDER
			CopyDiscordSharpPlusDependencies( configuration.directory );
#endif
			Recorder = new MatchRecorderHandler( configuration.directory );
			HarmonyInstance.Create( GetType().Namespace ).PatchAll( Assembly.GetExecutingAssembly() );
		}

		private Assembly ModResolve( object sender , ResolveEventArgs args )
		{
			string dllFolder = Path.GetFileNameWithoutExtension( GetType().Assembly.ManifestModule?.ScopeName ?? GetType().Namespace );
			string cleanName = args.Name.Split( ',' ) [0];
			//now try to load the requested assembly


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