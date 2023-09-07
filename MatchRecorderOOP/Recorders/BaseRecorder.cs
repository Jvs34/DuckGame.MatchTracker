using MatchRecorder.Services;
using MatchRecorderShared.Messages;
using MatchTracker;
using Microsoft.Extensions.Logging;

namespace MatchRecorder.Recorders
{
	internal abstract class BaseRecorder : IRecorder
	{
		public abstract bool IsRecording { get; }
		public abstract RecordingType ResultingRecordingType { get; set; }
		public MatchData CurrentMatch { get; set; }
		public RoundData CurrentRound { get; set; }

		/// <summary>
		/// Data that has arrived from network messages yet to be processed
		/// </summary>
		protected MatchData PendingMatchData { get; set; } = new MatchData();

		/// <summary>
		/// Data that has arrived from network messages yet to be processed
		/// </summary>
		protected RoundData PendingRoundData { get; set; } = new RoundData();

		protected ILogger<IRecorder> Logger { get; set; }
		protected IGameDatabase GameDatabase { get; set; }
		protected ModMessageQueue MessageQueue { get; set; }

		public BaseRecorder(
			ILogger<IRecorder> logger ,
			IGameDatabase db,
			ModMessageQueue messageQueue
			)
		{
			Logger = logger;
			GameDatabase = db;
			MessageQueue = messageQueue;
		}

		#region ABSTRACT
		public abstract void StartRecordingMatch();
		public abstract void StartRecordingRound();
		public abstract void StopRecordingMatch();
		public abstract void StopRecordingRound();
		public abstract void Update();
		#endregion

		public void SendHUDmessage( string message )
		{
			Logger.LogInformation( "Sending to client: {message}" , message );
			MessageQueue.ClientMessageQueue.Enqueue( new ClientHUDMessage()
			{
				Message = message
			} );
		}
	}
}
