using MatchRecorderShared.Messages;
using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MatchRecorder;

internal class ModMessageQueue : IAsyncDisposable, IDisposable
{
	private bool IsDisposed { get; set; }

	public ConcurrentQueue<BaseMessage> RecorderMessageQueue { get; } = new();
	public ConcurrentQueue<TextMessage> ClientMessageQueue { get; } = new();

	public Channel<BaseMessage> ClientToRecorderChannel { get; } = Channel.CreateUnbounded<BaseMessage>( new UnboundedChannelOptions()
	{
		SingleReader = true , //only the background service will read this
		SingleWriter = false , //multiple endpoints might try to add stuff to this for whatever reason
	} );

	public Channel<BaseMessage> RecorderToClientChannel { get; } = Channel.CreateUnbounded<BaseMessage>( new UnboundedChannelOptions()
	{
		SingleReader = false ,
		SingleWriter = true ,
	} );

	public ModMessageQueue()
	{

	}

	public void PushToRecorderQueue( BaseMessage message ) => RecorderMessageQueue.Enqueue( message );
	public void PushToClientMessageQueue( TextMessage message ) => ClientMessageQueue.Enqueue( message );

	protected virtual void Dispose( bool disposing )
	{
		if( !IsDisposed )
		{
			if( disposing )
			{
				// TODO: dispose managed state (managed objects)
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			IsDisposed = true;
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose( disposing: true );
		GC.SuppressFinalize( this );
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCore();

		Dispose( false );

		// Suppress finalization.
		GC.SuppressFinalize( this );
	}

	private ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
}
