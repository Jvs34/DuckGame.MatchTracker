namespace MatchRecorder
{
	interface IRecorder
	{
		bool IsRecording { get; set; }

		void Initialize();
		void Update();
		void StartRecording();
		void StopRecording();
	}
}
