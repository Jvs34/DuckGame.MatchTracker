using System.Threading.Tasks;
using MatchTracker;

namespace MatchUploader.Uploaders
{
	public class DiscordUploader : Uploader
	{
		public DiscordUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase , UploaderSettings settings ) : base( uploaderInfo , gameDatabase , settings )
		{
		}

		public override async Task Initialize()
		{
		}

		protected override async Task<PendingUpload> CreatePendingUpload( IDatabaseEntry entry )
		{
			return null;
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
