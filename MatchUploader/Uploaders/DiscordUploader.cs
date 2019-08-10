using MatchTracker;
using System.Threading.Tasks;

namespace MatchUploader.Uploaders
{
	public class DiscordUploader : Uploader
	{
		public DiscordUploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase , UploaderSettings settings ) : base( uploaderInfo , gameDatabase , settings )
		{
		}

		public override async Task Initialize()
		{
			await Task.CompletedTask;
		}

		protected override async Task<PendingUpload> CreatePendingUpload( IDatabaseEntry entry )
		{
			await Task.CompletedTask;
			return null;
		}

		protected override async Task FetchUploads()
		{
			await Task.CompletedTask;
		}

		protected override async Task<bool> UploadItem( PendingUpload upload )
		{
			await Task.CompletedTask;
			return true;
		}
	}
}
