using Firebase.Database;
using Firebase.Database.Offline;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MatchTracker
{
	public class FirebaseGameDatabase : IGameDatabase
	{
		public SharedSettings SharedSettings { get; set; }

		public bool ReadOnly => false;

		private FirebaseClient FirebaseClient { get; }

		public FirebaseGameDatabase( string firebaseUrl  )
		{
			FirebaseClient = new FirebaseClient( firebaseUrl , new FirebaseOptions()
			{
				OfflineDatabaseFactory = ( t , s ) => new OfflineDatabase( t , s ) ,
			} );

			var gay = new OfflineDatabase( typeof( GlobalData ) , ""  );
			var gay2 = gay [""];
		}

		public async Task<T> GetData<T>( string dataId = "" ) where T : IDatabaseEntry
		{
			throw new NotImplementedException();
		}

		public async Task<GlobalData> GetGlobalData( bool forceRefresh = false )
		{
			return await GetData<GlobalData>();
		}

		public async Task<MatchData> GetMatchData( string matchName , bool forceRefresh = false )
		{
			return await GetData<MatchData>( matchName );
		}

		public async Task<RoundData> GetRoundData( string roundName , bool forceRefresh = false )
		{
			return await GetData<RoundData>( roundName );
		}

		public async Task IterateOverAllRoundsOrMatches( bool matchOrRound , Func<IWinner , Task> callback )
		{
			throw new NotImplementedException();
		}

		public async Task Load()
		{
			//firebase connect
		}

		public async Task SaveData<T>( T data ) where T : IDatabaseEntry
		{
			//firebase save
		}

		public async Task SaveGlobalData( GlobalData globalData )
		{
			await SaveData( globalData );
		}

		public async Task SaveMatchData( string matchName , MatchData matchData )
		{
			await SaveData( matchData );
		}

		public async Task SaveRoundData( string roundName , RoundData roundData )
		{
			await SaveData( roundData );
		}
	}
}
