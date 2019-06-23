using System;
using System.Collections.Generic;

namespace MatchUploader
{
	public class UploaderSettings
	{
		public KeyValueDataStore DataStore { get; set; } = new KeyValueDataStore();
		public string GitEmail { get; set; }
		public string GitPassword { get; set; }
		public string GitUsername { get; set; }
		public Dictionary<MatchTracker.VideoMirrorType , UploaderInfo> UploadersInfo { get; set; } = new Dictionary<MatchTracker.VideoMirrorType , UploaderInfo>();
		public int RetryCount { get; set; } = 5;
		public GoogleSecrets Secrets { get; set; }
		public Uri YoutubeChannel { get; set; }
		public string CalendarID { get; set; }

		/// <summary>
		/// Default to the youtube upload behaviour
		/// </summary>
		public MatchTracker.VideoMirrorType VideoMirrorUpload { get; set; }
		public int DiscordMaxUploadSize { get; set; } = 8388608;
		public ulong DiscordUploadChannel { get; set; }
		public string CronSchedule { get; set; }
		public DateTime LastRan { get; set; } = DateTime.Now.Subtract( TimeSpan.FromSeconds( 1 ) );
		public bool ScheduleEnabled { get; set; }
	}
}