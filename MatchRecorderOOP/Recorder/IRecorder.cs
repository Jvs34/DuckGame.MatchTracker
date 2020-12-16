using MatchTracker;

namespace MatchRecorder
{
	internal interface IRecorder
	{
		/// <summary>
		/// Whether or not the recorder is doing an actual recording of any sorts
		/// </summary>
		bool IsRecording { get; }
		RecordingType ResultingRecordingType { get; set; }
		void StartRecordingMatch();
		void StopRecordingMatch();
		void StartRecordingRound();
		void StopRecordingRound();
		void Update();
	}
}