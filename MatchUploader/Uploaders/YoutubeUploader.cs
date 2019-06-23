using MatchTracker;
using System.Threading.Tasks;

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
