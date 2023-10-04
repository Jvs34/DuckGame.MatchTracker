using HarmonyLib;
using System;
using System.Reflection;

namespace MatchRecorder
{
	public class MatchRecorderMod : DuckGame.ClientMod, IDisposable
	{
		private bool IsDisposed { get; set; }

		public static MatchRecorderMod Instance => DuckGame.ModLoader.GetMod<MatchRecorderMod>();
		public MatchRecorderClient Recorder { get; set; }
		private Harmony HarmonyInstance { get; set; }

		public MatchRecorderMod()
		{
			HarmonyInstance = new Harmony( GetType().Namespace );
		}

		protected override void OnPostInitialize()
		{
			HarmonyInstance.PatchAll( Assembly.GetExecutingAssembly() );
			Recorder = new MatchRecorderClient( configuration.directory );
		}

		protected virtual void Dispose( bool disposing )
		{
			if( !IsDisposed )
			{
				if( disposing )
				{
					Recorder.Dispose();
					Recorder = null;
				}
				IsDisposed = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose( disposing: true );
			GC.SuppressFinalize( this );
		}
	}
}