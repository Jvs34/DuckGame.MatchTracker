using MatchRecorderShared.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace MatchRecorderShared
{
	public interface IRecorderSharedSettings
	{

		/// <summary>
		/// Whether to instatiate a recorder at all
		/// </summary>
		bool RecordingEnabled { get; set; }

		/// <summary>
		/// The recorder type used in the out of process program
		/// </summary>
		RecorderType RecorderType { get; set; }
	}
}
