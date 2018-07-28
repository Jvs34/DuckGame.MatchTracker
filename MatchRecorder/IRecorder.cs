namespace MatchRecorder
{
	interface IRecorder
	{
		bool IsRecording { get; }

		void Update();
		void StartRecording();
		void StopRecording();
	}
}
