using MatchTracker;

namespace MatchRecorder
{
	interface IRecorder
	{
		RecordingType ResultingRecordingType { get; set; }
		bool IsRecording { get; }

		void Update();
		void StartRecording();
		void StopRecording();
	}
}
