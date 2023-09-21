using MatchRecorderShared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared
{
	/// <summary>
	/// Settings used by the client mod
	/// </summary>
	public class ModSettings : IRecorderSharedSettings
	{
		public bool RecordingEnabled {  get; set; }
		public RecorderType RecorderType { get; set; }
	}
}
