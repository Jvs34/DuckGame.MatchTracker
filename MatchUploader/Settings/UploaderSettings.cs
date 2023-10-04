using System.Collections.Generic;

namespace MatchUploader
{
	public class UploaderSettings
	{
		public Dictionary<string , UploaderInfo> UploadersInfo { get; set; } = new Dictionary<string , UploaderInfo>();
		public int RetryCount { get; set; } = 5;
		public GoogleSecrets GoogleSecrets { get; set; }
		public KeyValueDataStore GoogleDataStore { get; set; } = new KeyValueDataStore();
		public uint MaxKilobytesPerSecond { get; set; } = 0;
	}
}