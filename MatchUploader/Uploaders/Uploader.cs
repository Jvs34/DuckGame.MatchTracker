using MatchTracker;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MatchUploader
{
	public abstract class Uploader
	{
		public UploaderInfo Info { get; }
		protected IGameDatabase DB { get; }
		protected Queue<PendingUpload> Uploads { get; } = new Queue<PendingUpload>();

		public UploaderSettings UploaderSettings { get; }

		public bool DoFetchUploads { get; set; } = true;

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

		public event Action SaveSettingsCallback;

		public event Func<string , Task> UpdateStatusCallback;

		protected Uploader( UploaderInfo uploaderInfo , IGameDatabase gameDatabase , UploaderSettings settings )
		{
			Info = uploaderInfo;
			DB = gameDatabase;
			UploaderSettings = settings;
		}

		public abstract Task Initialize();

		public virtual void CreateDefaultInfo()
		{

		}

		protected abstract Task<PendingUpload> CreatePendingUpload( IDatabaseEntry entry );

		protected abstract Task FetchUploads();

		public async Task AddUpload( IDatabaseEntry databaseEntry )
		{
			var pendingUpload = await CreatePendingUpload( databaseEntry );

			if( pendingUpload != null )
			{
				Uploads.Enqueue( pendingUpload );
			}
		}

		protected void CheckLimits()
		{
			if( Info.NextResetTime < DateTime.Now )
			{
				Info.CurrentUploads = 0;
			}
		}

		protected bool CanUpload()
		{
			if( !Info.HasApiLimit )
			{
				return true;
			}

			CheckLimits();

			//check if the time has beenset

			return Info.CurrentUploads <= Info.UploadsBeforeReset;
		}

		public virtual async Task UploadAll()
		{
			if( DoFetchUploads )
			{
				await UpdateStatus( $"{GetType().Name}: Fetching uploads" );

				//if there's a valid currentupload, add it here again
				if( CurrentUpload != null )
				{
					Uploads.Enqueue( CurrentUpload );
				}

				await FetchUploads();
			}

			await UpdateStatus( $"{GetType().Name}: Uploading {Uploads.Count} items" );

			while( Uploads.Count > 0 && CanUpload() )
			{
				PendingUpload upload = Uploads.Dequeue();

				CurrentUpload = upload;

				SaveSettings();
				bool uploadCompleted;
				bool shouldRetry;

				do
				{
					Info.LastUploadTime = DateTime.Now;
					Info.CurrentUploads++;

					await UpdateStatus( $"{GetType().Name}:  {Info.CurrentUploads} / {Info.UploadsBeforeReset}" );
					uploadCompleted = await UploadItem( upload );

					upload.ErrorCount = uploadCompleted ? 0 : upload.ErrorCount + 1;

					if( uploadCompleted && Info.HasApiLimit )
					{
						Info.NextResetTime = Info.LastUploadTime.Add( Info.NextReset );
					}

					if( Info.Retries > 0 )
					{
						shouldRetry = uploadCompleted ? !uploadCompleted : upload.ErrorCount < Info.Retries;
					}
					else
					{
						shouldRetry = false;
					}

				}
				while( shouldRetry );

				CurrentUpload = null;
				SaveSettings();
			}
		}

		protected abstract Task<bool> UploadItem( PendingUpload upload );

		protected void SaveSettings()
		{
			SaveSettingsCallback.Invoke();
		}

		protected async Task UpdateStatus( string status )
		{
			Console.WriteLine( status );
			await UpdateStatusCallback.Invoke( status );
		}
	}
}
