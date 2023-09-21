using MatchRecorderShared.Enums;
using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared
{
	/// <summary>
	/// Settings used by the out of process program
	/// </summary>
	public class RecorderSettings : IRecorderSharedSettings
	{
		/// <summary>
		/// Used to check whether the parent process is still alive, otherwise we'll autoclose
		/// </summary>
		public int DuckGameProcessID { get; set; }
		public bool AutoCloseWhenParentDies { get; set; }
		public bool RecordingEnabled { get; set; }
		public RecorderType RecorderType { get; set; }
	}
}
