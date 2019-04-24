namespace MatchTracker
{
	public enum VideoMirrorType
	{
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
		//Streamable //maybe not streamable, it deletes video without any views after 30 days or something
	}
}
