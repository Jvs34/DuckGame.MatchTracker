namespace MatchTracker
{
	public enum VideoMirrorType
	{
		None = -1,

		/// <summary>
		/// Default behaviour
		/// </summary>
		Youtube = 0,

		/// <summary>
		/// 8 mb file upload limit, not sure about daily quotas and whatnot
		/// </summary>
		Discord,

		/// <summary>
		/// 100 videos every day limit
		/// </summary>
		Twitch,

		/// <summary>
		/// maybe not streamable, it deletes video without any views after 30 days or something
		/// </summary>
		Streamable,

		/// <summary>
		/// This can get throttled apparently, and it'll spit out a 403
		/// </summary>
		OneDrive,

		/// <summary>
		/// A raw link to the file hosted somewhere
		/// </summary>
		Http,
	}
}
