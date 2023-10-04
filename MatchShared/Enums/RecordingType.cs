using System;

namespace MatchTracker;

public enum RecordingType
{
	/// <summary>
	/// No video was ever recorded for this
	/// </summary>
	None,

	/// <summary>
	/// A video file saved as <seealso cref="SharedSettings.GetRoundVideoPath"/> or <seealso cref="SharedSettings.GetMatchVideoPath"/>
	/// </summary>
	Video,


	/// <summary>
	/// Records a replay of the match, might break between DuckGame versions
	/// </summary>
	[Obsolete( "Unused" )]
	Replay,


	/// <summary>
	/// Same as <see cref="Replay"/>, but additionally records the discord voicechat to a file.
	/// </summary>
	[Obsolete( "Unused" )]
	ReplayAndVoiceChat,
}