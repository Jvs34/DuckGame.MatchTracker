using MatchTracker;
using System.Threading.Tasks;

namespace MatchRecorder.Recorders
{
	internal interface IRecorder
	{
		/// <summary>
		/// Whether or not the recorder is doing an actual recording of any sorts
		/// </summary>
		bool IsRecording { get; protected set; }
		RecordingType ResultingRecordingType { get; set; }
		Task StartRecordingMatch( IPlayersList playersList , ITeamsList teamsList );
		Task StopRecordingMatch( IPlayersList playersList , ITeamsList teamsList , IWinner winner );
		Task StartRecordingRound( ILevelName levelName , IPlayersList playersList , ITeamsList teamsList );
		Task StopRecordingRound( IPlayersList playersList , ITeamsList teamsList , IWinner winner );
		Task Update();
		void SendHUDmessage( string message );
	}
}