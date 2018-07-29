using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchRecorder
{
	/// <summary>
	/// A class used for the purpose of receiving duck game's recorder events
	/// used for either video or highlight recordings
	/// </summary>
	public class DuckRecordingListener : DuckGame.Recording
	{
		public DuckRecordingListener(): base()
		{

		}
	}
}
