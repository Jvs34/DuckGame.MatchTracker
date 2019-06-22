using MatchTracker;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MatchUploader
{
	public abstract class Uploader
	{
		protected UploaderInfo Info { get; }

		protected IGameDatabase DB { get; }

		protected Queue<PendingUpload> Uploads => Info.Uploads;

		protected Uploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase )
		{
			Info = uploaderInfo;
			DB = gameDatabase;
		}

		public abstract Task Initialize();

		protected PendingUpload CreatePendingUpload( string roundName )
		{
			return new PendingUpload()
			{
				DataName = roundName ,
			};
		}

		protected abstract Task FetchUploads();

		public virtual async Task UploadAll()
		{
			while( Uploads.Count > 0 )
			{
				PendingUpload upload = Uploads.Peek();

				bool canContinue = false;
				bool uploadCompleted = false;

				while( !canContinue )
				{
					uploadCompleted = await UploadItem( upload );

					canContinue = uploadCompleted
						? uploadCompleted
						: upload.ErrorCount >= Info.Retries;
				}

				if( uploadCompleted )
				{
					Uploads.Dequeue();
				}

				await SaveSettings();
			}
		}

		protected abstract Task<bool> UploadItem( PendingUpload upload );

		protected async Task SaveSettings()
		{

		}
	}
}
