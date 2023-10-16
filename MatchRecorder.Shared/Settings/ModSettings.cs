using MatchRecorder.Shared.Enums;
using MatchRecorder.Shared.Interfaces;

namespace MatchRecorder.Shared.Settings;

/// <summary>
/// Settings used by the client mod
/// </summary>
public class ModSettings : IRecorderSharedSettings
{
	public bool RecordingEnabled { get; set; }
	public RecorderType RecorderType { get; set; }
	public bool HideOOPWindow { get; set; }
}
