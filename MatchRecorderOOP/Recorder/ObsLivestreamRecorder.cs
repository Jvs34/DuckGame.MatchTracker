using MatchTracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatchRecorder
{
	public class ObsLivestreamRecorder : IRecorder
	{
		public bool IsRecording => false;
		public RecordingType ResultingRecordingType { get; set; }

		public void StartRecordingMatch()
		{
		}

		public void StartRecordingRound()
		{
		}

		public void StopRecordingMatch()
		{
		}

		public void StopRecordingRound()
		{
		}

		public void Update()
		{
		}
	}
}
