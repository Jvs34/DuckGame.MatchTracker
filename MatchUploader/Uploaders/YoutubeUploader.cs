using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MatchTracker;

namespace MatchUploader
{
	public class YoutubeUploader : Uploader
	{
		public YoutubeUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase ) : base( uploaderInfo , gameDatabase )
		{
		}

		public override async Task Initialize()
		{

		}

		protected override async Task FetchUploads()
		{
		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{

			return true;
		}
	}
}
