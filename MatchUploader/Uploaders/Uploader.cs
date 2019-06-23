﻿using MatchTracker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace MatchUploader
{
	public abstract class Uploader
	{
		public UploaderInfo Info { get; }
		protected IGameDatabase DB { get; }
		protected Queue<PendingUpload> Uploads { get; } = new Queue<PendingUpload>();

		public UploaderSettings UploaderSettings { get; }

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

		public event Func<Task<string>> UpdateStatus;

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

		protected async Task<PendingUpload> CreatePendingUpload( string roundName )
		{
			return CreatePendingUpload( await DB.GetData<RoundData>( roundName ) );
		}

		protected PendingUpload CreatePendingUpload( RoundData roundData )
		{
			PendingUpload upload = null;

			if( roundData != null )
			{
				string videoPath = DB.SharedSettings.GetRoundVideoPath( roundData.DatabaseIndex , false );

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
						DataName = roundData.DatabaseIndex ,
						FileSize = fileInfo.Length ,
					};
				}
			}

			return upload;
		}

		protected abstract Task FetchUploads();

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
			//if there's a valid currentupload, add it here again
			if( CurrentUpload != null )
			{
				Uploads.Enqueue( CurrentUpload );
			}

			Console.WriteLine( $"Fetching uploads for {GetType().Name}" );

			await FetchUploads();

			Console.WriteLine( $"Uploading {Uploads.Count} rounds" );

			while( Uploads.Count > 0 && CanUpload() )
			{
				PendingUpload upload = Uploads.Dequeue();

				CurrentUpload = upload;

				await SaveSettings();
				bool uploadCompleted;
				bool shouldRetry;

				do
				{
					Info.LastUploadTime = DateTime.Now;
					Info.CurrentUploads++;

					uploadCompleted = await UploadItem( upload );

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
