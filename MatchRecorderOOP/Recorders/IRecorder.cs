using MatchTracker;
using System.Threading.Tasks;

namespace MatchRecorder.Recorders
{
	internal interface IRecorder
	{
		/// <summary>
		/// Whether or not the recorder is doing an actual recording of any sorts
		/// </summary>
		bool IsRecording { get; }
		RecordingType ResultingRecordingType { get; set; }
		Task StartRecordingMatch();
		Task StopRecordingMatch();
		Task StartRecordingRound();
		Task StopRecordingRound();
		Task Update();
	}
}