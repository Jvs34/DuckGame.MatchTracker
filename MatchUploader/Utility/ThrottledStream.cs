using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MatchUploader
{
	public class ThrottledStream : Stream
	{
		protected const int TimeUnit = 1000;
		public int BPS { get; }
		public int BytesProcessed { get; protected set; }
		protected Stream BaseStream { get; }
		protected Stopwatch Stopwatch { get; }
		public override bool CanRead => BaseStream.CanRead;
		public override bool CanSeek => BaseStream.CanSeek;
		public override bool CanWrite => BaseStream.CanWrite;
		public override long Length => BaseStream.Length;
		public override long Position { get => BaseStream.Position; set => BaseStream.Position = value; }

		public ThrottledStream( Stream stream , int bytesPerSecond = -1 )
		{
			BaseStream = stream;
			BPS = bytesPerSecond;
			Stopwatch = new Stopwatch();
		}

		public override int Read( byte[] buffer , int offset , int count )
		{
			if( BPS == -1 )
			{
				return BaseStream.Read( buffer , offset , count );
			}

			CheckWatch();

			//check if reading with this count would go over the limit
			if( IsLimitReached( count ) )
			{
				//then cap it to the remaining bytes we have remaining for this time
				count = Math.Clamp( count , 0 , BPS );
			}

			int bytesRead = BaseStream.Read( buffer , offset , count );

			BytesProcessed += bytesRead;

			Throttle();

			return bytesRead;
		}

		public override void Write( byte[] buffer , int offset , int count )
		{
			if( BPS == -1 )
			{
				BaseStream.Write( buffer , offset , count );
				return;
			}

			CheckWatch();

			//check if reading with this count would go over the limit
			if( IsLimitReached( count ) )
			{
				//then cap it to the remaining bytes we have remaining for this time
				count = Math.Clamp( count , 0 , BPS );
			}

			BaseStream.Write( buffer , offset , count );

			BytesProcessed += count;

			Throttle();
		}

		protected void Throttle()
		{
			var remainingTime = GetWaitingTime();
			if( remainingTime > 0 )
			{
				var source = new CancellationTokenSource();
				source.CancelAfter( remainingTime );
				source.Token.WaitHandle.WaitOne( TimeUnit * 5 );
				Stopwatch.Restart();
				BytesProcessed = 0;
			}
		}

		protected async Task ThrottleAsync()
		{
			var remainingTime = GetWaitingTime();
			if( remainingTime > 0 )
			{
				await Task.Delay( remainingTime );
				Stopwatch.Restart();
				BytesProcessed = 0;
			}
		}

		protected bool IsLimitReached( int bytes = 0 ) => ( BytesProcessed + bytes ) >= BPS;

		protected int GetWaitingTime()
		{
			if( IsLimitReached() )
			{
				//now we wait for the remaining time on the stopwatch
				var remainingTime = Stopwatch.ElapsedMilliseconds - TimeUnit;

				if( remainingTime < 0 )
				{
					return ( int ) Math.Abs( remainingTime );
				}
			}
			return 0;
		}

		protected void CheckWatch()
		{
			if( !Stopwatch.IsRunning )
			{
				Stopwatch.Restart();
			}
		}

		public override void Flush() => BaseStream.Flush();
		public override long Seek( long offset , SeekOrigin origin ) => BaseStream.Seek( offset , origin );
		public override void SetLength( long value ) => BaseStream.SetLength( value );
	}
}
