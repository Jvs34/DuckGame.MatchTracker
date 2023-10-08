using MatchRecorderShared.Enums;

namespace MatchRecorderShared;

/// <summary>
/// Settings used by the client mod
/// </summary>
public class ModSettings : IRecorderSharedSettings
{
	public bool RecordingEnabled {  get; set; }
	public RecorderType RecorderType { get; set; }
}
