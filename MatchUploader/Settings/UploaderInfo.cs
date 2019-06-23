using MatchTracker;
using System;

namespace MatchUploader
{
	public class UploaderInfo
	{
		/// <summary>
		/// Youtube, twitch etc etc
		/// </summary>
		public VideoMirrorType UploaderType { get; set; }

		/// <summary>
		/// Even if this uploader has no theoretical limit, set this anyway
		/// Don't want to get banned by surprise because of abuse
		/// </summary>
		public int UploadsBeforeReset { get; set; }

		/// <summary>
		/// The uploads that have been done, or depending on the API, initiated before the reset
		/// 
		/// This is not used if HasApiLimit is false
		/// </summary>
		public int CurrentUploads { get; set; }

		/// <summary>
		/// Whether or not we have to check for api limits and whatnot
		/// </summary>
		public bool HasApiLimit { get; set; }

		/// <summary>
		/// Set at the start of a download start/resume operation, that's what youtube tracks, at least
		/// </summary>
		public DateTime LastUploadTime { get; set; }

		/// <summary>
		/// This is set when the first download starts or resumes
		/// </summary>
		public DateTime NextResetTime { get; set; }


		/// <summary>
		/// Depending on the API, this might be 24 hours or whatever
		/// </summary>
		public TimeSpan NextReset { get; set; }

		public int Retries { get; set; }

		public PendingUpload CurrentUpload { get; set; }
	}
}
