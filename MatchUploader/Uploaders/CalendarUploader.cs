using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using MatchTracker;

namespace MatchUploader
{
	public class CalendarUploader : Uploader
	{
		public CalendarUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase ) : base( uploaderInfo , gameDatabase )
		{
		}

		public override Task Initialize()
		{
			throw new NotImplementedException();
		}

		protected override Task FetchUploads()
		{
			throw new NotImplementedException();
		}

		protected override Task<bool> UploadItem( PendingUpload upload )
		{
			throw new NotImplementedException();
		}
	}
}
