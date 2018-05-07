using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using MatchTracker;

namespace MatchViewer
{
	public interface IMatchDatabase
	{

	}

    public class MatchDatabase : IMatchDatabase
    {
		private HttpClient client;
		private String baseRepositoryURL;
		private SharedSettings sharedSettings;

		public MatchDatabase( HttpClient givenClient )
		{
			givenClient = client;
			baseRepositoryURL = "https://raw.githubusercontent.com/Jvs34/DuckGame.MatchDB/master/";
		}
    }
}
