namespace MatchTracker
{
	public enum VideoType
	{
		None,

		/// <summary>
		/// A playlist containing multiple videos
		/// </summary>
		PlaylistLink,

		/// <summary>
		/// A singular video
		/// </summary>
		VideoLink,

		/// <summary>
		/// A video with multiple videos one after another WITHOUT pause
		/// </summary>
		MergedVideoLink,

		/// <summary>
		/// A livestream with multiple videos, pauses between videos occur
		/// </summary>
		LivestreamLink,
	}
}