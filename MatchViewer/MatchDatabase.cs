using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MatchTracker;
using Flurl;

namespace MatchViewer
{
	public interface IMatchDatabase
	{
		Task<GlobalData> GetGlobalData();

		Task<MatchData> GetMatchData( String matchName );

		Task<RoundData> GetRoundData( String roundName );

	}

    public class MatchDatabase : IMatchDatabase
    {
		private HttpClient client;
		private Uri baseRepositoryURL;
		private SharedSettings sharedSettings;

		public MatchDatabase( HttpClient givenClient )
		{
			givenClient = client;
			baseRepositoryURL = new Uri("https://raw.githubusercontent.com/Jvs34/DuckGame.MatchDB/master/");
		}

		public Task<GlobalData> GetGlobalData()
		{
			throw new NotImplementedException();
		}

		public Task<MatchData> GetMatchData( string matchName )
		{
			throw new NotImplementedException();
		}

		public Task<RoundData> GetRoundData( string roundName )
		{
			throw new NotImplementedException();
		}
	}
}
