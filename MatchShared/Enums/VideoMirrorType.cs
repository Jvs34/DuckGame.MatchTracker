using System;

namespace MatchTracker
{
	public enum VideoServiceType
	{
		None,

		/// <summary>
		/// Default behaviour
		/// </summary>
		Youtube,

		/// <summary>
		/// A raw link to the file hosted somewhere
		/// </summary>
		Http,

		/// <summary>
		/// 50 mb file upload limit, not sure about daily quotas and whatnot
		/// <para>
		/// A link to this is a link to the message, not the raw url of the video
		/// </para>
		/// </summary>
		[Obsolete( "Unused" )]
		Discord,

		/// <summary>
		/// 100 videos every day limit, latest API literally does not have uploading support
		/// </summary>
		[Obsolete( "Unused" )]
		Twitch,

		/// <summary>
		/// Deletes video without any views after 30 days or something, unsuitable
		/// </summary>
		[Obsolete( "Unused" )]
		Streamable,

		/// <summary>
		/// This can get throttled apparently, and it'll spit out a 403
		/// </summary>
		[Obsolete( "Unused" )]
		OneDrive,
	}
}
