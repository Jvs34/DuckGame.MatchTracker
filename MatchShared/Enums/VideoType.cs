namespace MatchTracker
{
	public enum VideoUrlType
	{
		None,

		/// <summary>
		/// A playlist containing multiple videos, only used by <see cref="MatchData"/>
		/// </summary>
		PlaylistLink,

		/// <summary>
		/// A singular video, only used by <see cref="RoundData"/>
		/// </summary>
		VideoLink,

		/// <summary>
		/// A video spliced from individual clips cutting out pauses,
		/// <para>
		/// Likely done by a video merger
		/// </para>
		/// </summary>
		MergedVideoLink,

		/// <summary>
		/// A raw video recording, contains pauses and unrelated content
		/// </summary>
		RawVideoLink,

		/// <summary>
		/// A raw livestream, just like <see cref="RawVideoLink"/>, contains pauses and unrelated content
		/// </summary>
		LivestreamLink,
	}
}