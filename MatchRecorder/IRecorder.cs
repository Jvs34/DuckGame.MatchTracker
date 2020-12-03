using DuckGame;
using MatchTracker;

namespace MatchRecorder
{
	internal interface IRecorder
	{
		bool IsRecording { get; }
		RecordingType ResultingRecordingType { get; set; }
		void StartRecording( bool matchrecording = false );
		void StopRecording();
		void Update();
	}
}