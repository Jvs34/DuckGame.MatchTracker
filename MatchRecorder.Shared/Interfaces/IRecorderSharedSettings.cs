using MatchRecorder.Shared.Enums;

namespace MatchRecorder.Shared.Interfaces;

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
