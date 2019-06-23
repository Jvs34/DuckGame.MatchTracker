using MatchTracker;
using System;
using System.Threading.Tasks;

namespace MatchUploader
{
	public class CalendarUploader : Uploader
	{
		public CalendarUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase ) : base( uploaderInfo , gameDatabase )
		{
		}

		public override async Task Initialize()
		{
			Console.WriteLine( "Calendar uploader initialized" );
		}

		protected override async Task FetchUploads()
		{
			Console.WriteLine( "Calendar uploader fetched shit" );
		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{
			Console.WriteLine( "Calendar uploader uploaded shit" );
			return true;
		}
	}
}
