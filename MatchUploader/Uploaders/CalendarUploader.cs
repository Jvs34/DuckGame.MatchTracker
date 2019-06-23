using MatchTracker;
using System;
using System.Threading.Tasks;

namespace MatchUploader
{
	public class CalendarUploader : Uploader
	{
		public CalendarUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase , UploaderSettings settings ) : base( uploaderInfo , gameDatabase , settings )
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
			Console.WriteLine( "Calendar uploader uploaded shit" );
			return true;
		}
	}
}
