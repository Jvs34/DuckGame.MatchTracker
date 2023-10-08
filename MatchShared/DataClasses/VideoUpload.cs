using MatchShared.Enums;
using MatchShared.Interfaces;
using System;

namespace MatchShared.DataClasses;

/// <summary>
/// A video upload for either a round or match
/// </summary>
public class VideoUpload : IStartEndTime
{
	public string Url { get; set; }

	/// <summary>
	/// The service used to upload this video
	/// </summary>
	public VideoServiceType ServiceType { get; set; }

	/// <summary>
	/// The type of upload on that service
	/// </summary>
	public VideoUrlType VideoType { get; set; }

	/// <summary>
	/// The type of video, if at all, that was recorded
	/// </summary>
	public RecordingType RecordingType { get; set; }

	/// <summary>
	/// Time the recording started at, most of the time this is when the match/round started
	/// </summary>
	public DateTime TimeStarted { get; set; }

	/// <summary>
	/// Time the recording ended, most of the time this is when the match/round ended
	/// </summary>
	public DateTime TimeEnded { get; set; }

	public TimeSpan GetDuration() => TimeEnded.Subtract( TimeStarted );
	public bool IsPending() => VideoType != VideoUrlType.None && string.IsNullOrEmpty( Url );
}
