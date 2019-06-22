using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MatchUploader
{
	public abstract class VideoUploader
	{
		protected UploaderInfo Info { get; }

		protected IGameDatabase DB { get; }

		protected PendingUpload Uploads { get; }

		public VideoUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase )
		{
			Info = uploaderInfo;
			DB = gameDatabase;
		}

		protected abstract Task FetchUploads();
	}
}
