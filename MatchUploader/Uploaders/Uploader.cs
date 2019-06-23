using MatchTracker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MatchUploader
{
	public abstract class Uploader
	{
		protected UploaderInfo Info { get; }
		protected IGameDatabase DB { get; }
		protected Queue<PendingUpload> Uploads { get; } = new Queue<PendingUpload>();

		public PendingUpload CurrentUpload
		{
			get
			{
				return Info.CurrentUpload;
			}
			private set
			{
				Info.CurrentUpload = value;
			}
		}

		public double Progress
		{
			get
			{
				return CurrentUpload != null ? Math.Round( CurrentUpload.BytesSent / (double) CurrentUpload.FileSize * 100f , 2 ) : 0;
			}
		}

		public int Remaining
		{
			get
			{
				return Uploads.Count;
			}
		}

		public event Func<Task> SaveCallback;

		protected Uploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase )
		{
			Info = uploaderInfo;
			DB = gameDatabase;
		}

		public abstract Task Initialize();

		protected async Task<PendingUpload> CreatePendingUpload( string roundName )
		{
			PendingUpload upload = null;

			RoundData roundData = await DB.GetData<RoundData>( roundName );

			if( roundData != null )
			{
				string videoPath = DB.SharedSettings.GetRoundVideoPath( roundName , false );

				string reEncodedVideoPath = Path.ChangeExtension( videoPath , "converted.mp4" );

				if( File.Exists( reEncodedVideoPath ) )
				{
					videoPath = reEncodedVideoPath;
				}

				if( File.Exists( videoPath ) )
				{
					var fileInfo = new FileInfo( videoPath );

					upload = new PendingUpload()
					{
						DataName = roundName ,
						FileSize = fileInfo.Length ,
					};
				}
			}

			return upload;
		}

		protected abstract Task FetchUploads();

		public virtual async Task UploadAll()
		{
			//if there's a valid currentupload, add it here again
			if( CurrentUpload != null )
			{
				Uploads.Enqueue( CurrentUpload );
			}

			await FetchUploads();


			if( Info.HasApiLimit )
			{



			}



			while( Uploads.Count > 0 )
			{
				PendingUpload upload = Uploads.Dequeue();

				CurrentUpload = upload;

				await SaveSettings();

				bool shouldRetry;
				bool uploadCompleted;

				do
				{
					Info.LastUploadTime = DateTime.Now;
					Info.CurrentUploads++;
					uploadCompleted = await UploadItem( upload );

					shouldRetry = uploadCompleted
						? !uploadCompleted
						: upload.ErrorCount < Info.Retries;
				}
				while( shouldRetry );

				CurrentUpload = null;

				await SaveSettings();
			}
		}

		protected abstract Task<bool> UploadItem( PendingUpload upload );

		protected async Task SaveSettings()
		{
			await SaveCallback.Invoke();
		}
	}
}
