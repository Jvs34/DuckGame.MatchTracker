using Microsoft.Extensions.Configuration;
using NCrontab;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MatchUploader
{
	public class UploaderScheduler
	{
		private IConfigurationRoot Configuration { get; }

		private MatchUploaderHandler UploaderHandler { get; }

		private UploaderSettings Settings { get; } = new UploaderSettings();

		public UploaderScheduler( string [] args )
		{
			Configuration = new ConfigurationBuilder()
				.SetBasePath( Path.Combine( Directory.GetCurrentDirectory() , "Settings" ) )
				.AddJsonFile( "uploader.json" )
				.AddCommandLine( args )
			.Build();

			Configuration.Bind( Settings );

			UploaderHandler = new MatchUploaderHandler( args );
		}

		public async Task RunAsync()
		{
			//parse the cron schedule anyway
			CrontabSchedule schedule = CrontabSchedule.TryParse( Settings.CronSchedule );

			if( Settings.ScheduleEnabled && schedule != null )
			{
				while( true )
				{
					DateTime startTimeCheck = DateTime.Now;
					DateTime endTimeCheck = startTimeCheck.AddMinutes( 5 );

					DateTime nextOccurrence = schedule.GetNextOccurrence( Settings.LastRan );

					if( nextOccurrence > startTimeCheck && nextOccurrence < endTimeCheck )
					{
						await UploaderHandler.RunAsync();
					}

					await Task.Delay( TimeSpan.FromSeconds( 30 ) );
				}
			}
			else
			{

				await UploaderHandler.RunAsync();
			}
		}
	}
}
