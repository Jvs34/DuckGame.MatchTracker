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
	}
}
